import { FormControl, FormGroup, Validators } from '@angular/forms';

import {
  Device,
  DeviceType,
  GenerateDeviceDescriptionRequest,
  UpsertDeviceRequest
} from '../../core/models/device.models';

export type DeviceFormGroup = FormGroup<{
  name: FormControl<string>;
  manufacturer: FormControl<string>;
  type: FormControl<DeviceType>;
  operatingSystem: FormControl<string>;
  osVersion: FormControl<string>;
  processor: FormControl<string>;
  ramAmountGb: FormControl<number | null>;
  description: FormControl<string>;
  location: FormControl<string>;
}>;

export function createDeviceForm(): DeviceFormGroup {
  return new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(120)]
    }),
    manufacturer: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(120)]
    }),
    type: new FormControl<DeviceType>('phone', {
      nonNullable: true,
      validators: [Validators.required]
    }),
    operatingSystem: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(120)]
    }),
    osVersion: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(50)]
    }),
    processor: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(120)]
    }),
    ramAmountGb: new FormControl<number | null>(8, {
      validators: [Validators.required, Validators.min(1), Validators.max(1024)]
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(1000)]
    }),
    location: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(120)]
    })
  });
}

export function buildDeviceRequest(formGroup: DeviceFormGroup): UpsertDeviceRequest {
  const rawValue = formGroup.getRawValue();

  return {
    name: rawValue.name.trim(),
    manufacturer: rawValue.manufacturer.trim(),
    type: rawValue.type,
    operatingSystem: rawValue.operatingSystem.trim(),
    osVersion: rawValue.osVersion.trim(),
    processor: rawValue.processor.trim(),
    ramAmountGb: Number(rawValue.ramAmountGb),
    description: rawValue.description.trim(),
    location: rawValue.location.trim(),
    assignedUserId: null
  };
}

export function buildGenerateDeviceDescriptionRequest(
  formGroup: DeviceFormGroup
): GenerateDeviceDescriptionRequest | null {
  const rawValue = formGroup.getRawValue();

  const requiredTextValues = [
    rawValue.name,
    rawValue.manufacturer,
    rawValue.operatingSystem,
    rawValue.osVersion,
    rawValue.processor
  ];

  if (requiredTextValues.some((value) => !value.trim())) {
    formGroup.controls.name.markAsTouched();
    formGroup.controls.manufacturer.markAsTouched();
    formGroup.controls.operatingSystem.markAsTouched();
    formGroup.controls.osVersion.markAsTouched();
    formGroup.controls.processor.markAsTouched();
    return null;
  }

  const ramAmountGb = Number(rawValue.ramAmountGb);
  if (!Number.isFinite(ramAmountGb) || ramAmountGb < 1 || ramAmountGb > 1024) {
    formGroup.controls.ramAmountGb.markAsTouched();
    return null;
  }

  return {
    name: rawValue.name.trim(),
    manufacturer: rawValue.manufacturer.trim(),
    type: rawValue.type,
    operatingSystem: rawValue.operatingSystem.trim(),
    osVersion: rawValue.osVersion.trim(),
    processor: rawValue.processor.trim(),
    ramAmountGb
  };
}

export function findDuplicateDevice(
  devices: Device[],
  payload: UpsertDeviceRequest,
  excludedDeviceId: number | null
): Device | undefined {
  const payloadKey = createDuplicateKey(payload);

  return devices.find(
    (device) => device.id !== excludedDeviceId && createDuplicateKey(device) === payloadKey
  );
}

export function normalizeInventoryText(value: string): string {
  return value.trim().replace(/\s+/g, ' ').toLowerCase();
}

function createDuplicateKey(
  device: Pick<Device, 'name' | 'manufacturer' | 'type' | 'operatingSystem' | 'osVersion'>
): string {
  return [
    normalizeInventoryText(device.name),
    normalizeInventoryText(device.manufacturer),
    normalizeInventoryText(device.type),
    normalizeInventoryText(device.operatingSystem),
    normalizeInventoryText(device.osVersion)
  ].join('|');
}
