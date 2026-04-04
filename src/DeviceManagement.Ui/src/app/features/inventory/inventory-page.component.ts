import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import {
  CreateDeviceRequest,
  DeviceResponse,
  DEVICE_TYPE_OPTIONS,
  DeviceType,
  getDeviceTypeLabel
} from '../../core/models/device.models';
import { DeviceService } from '../../core/services/device.service';

@Component({
  selector: 'app-inventory-page',
  templateUrl: './inventory-page.component.html',
  styleUrl: './inventory-page.component.css',
  standalone: false
})
export class InventoryPageComponent implements OnInit {
  private readonly deviceService = inject(DeviceService);
  private readonly formBuilder = inject(FormBuilder);

  protected devices: DeviceResponse[] = [];
  protected isLoading = true;
  protected errorMessage = '';
  protected selectedDevice: DeviceResponse | null = null;
  protected selectedDeviceId: number | null = null;
  protected isDetailsLoading = false;
  protected detailsErrorMessage = '';
  protected readonly deviceTypeOptions = DEVICE_TYPE_OPTIONS;
  protected isCreateSubmitting = false;
  protected createErrorMessage = '';
  protected createSuccessMessage = '';
  protected readonly createDeviceForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    manufacturer: ['', [Validators.required, Validators.maxLength(120)]],
    type: [DeviceType.Phone, [Validators.required]],
    operatingSystem: ['', [Validators.required, Validators.maxLength(120)]],
    osVersion: ['', [Validators.required, Validators.maxLength(50)]],
    processor: ['', [Validators.required, Validators.maxLength(120)]],
    ramAmountGb: [8, [Validators.required, Validators.min(1), Validators.max(1024)]],
    description: ['', [Validators.required, Validators.maxLength(1000)]],
    location: ['', [Validators.required, Validators.maxLength(120)]]
  });

  ngOnInit(): void {
    this.loadDevices();
  }

  protected reloadDevices(): void {
    this.loadDevices();
  }

  protected getDeviceTypeLabel(type: DeviceResponse['type']): string {
    return getDeviceTypeLabel(type);
  }

  protected selectDevice(deviceId: number, forceReload = false): void {
    if (!forceReload && this.selectedDeviceId === deviceId && this.selectedDevice) {
      return;
    }

    this.selectedDeviceId = deviceId;
    this.loadSelectedDevice(deviceId);
  }

  protected isSelectedDevice(deviceId: number): boolean {
    return this.selectedDeviceId === deviceId;
  }

  protected submitCreateDevice(): void {
    this.createErrorMessage = '';
    this.createSuccessMessage = '';

    if (this.createDeviceForm.invalid) {
      this.createDeviceForm.markAllAsTouched();
      return;
    }

    const request = this.buildCreateDeviceRequest();

    if (this.hasDuplicateDevice(request)) {
      this.createErrorMessage =
        'A device with the same name, manufacturer, type, operating system, and OS version already exists.';
      return;
    }

    this.isCreateSubmitting = true;

    this.deviceService.createDevice(request).subscribe({
      next: (createdDevice) => {
        this.isCreateSubmitting = false;
        this.createSuccessMessage = `${createdDevice.name} was added to the inventory.`;
        this.selectedDeviceId = createdDevice.id;
        this.resetCreateDeviceForm();
        this.loadDevices();
      },
      error: (error: HttpErrorResponse) => {
        this.isCreateSubmitting = false;
        this.createErrorMessage =
          error.status === 409
            ? 'A matching device already exists in the inventory.'
            : 'We could not create the device right now.';
      }
    });
  }

  protected shouldShowCreateError(controlName: string): boolean {
    const control = this.createDeviceForm.get(controlName);
    return !!control && control.invalid && (control.touched || control.dirty);
  }

  protected getCreateErrorMessage(controlName: string): string {
    const control = this.createDeviceForm.get(controlName);

    if (!control?.errors) {
      return '';
    }

    if (control.errors['required']) {
      return 'This field is required.';
    }

    if (control.errors['maxlength']) {
      return 'This value is longer than the allowed limit.';
    }

    if (control.errors['min']) {
      return 'The value must be at least 1.';
    }

    if (control.errors['max']) {
      return 'The value is larger than the allowed limit.';
    }

    return 'This value is not valid.';
  }

  private loadDevices(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.deviceService.getDevices().subscribe({
      next: (devices) => {
        this.devices = [...devices].sort((left, right) => left.name.localeCompare(right.name));
        this.isLoading = false;

        if (this.devices.length === 0) {
          this.selectedDevice = null;
          this.selectedDeviceId = null;
          return;
        }

        const nextSelectedId = this.resolveSelectedDeviceId();
        this.selectDevice(nextSelectedId, true);
      },
      error: () => {
        this.errorMessage = 'We could not load the device inventory right now.';
        this.isLoading = false;
      }
    });
  }

  private loadSelectedDevice(deviceId: number): void {
    this.isDetailsLoading = true;
    this.detailsErrorMessage = '';

    this.deviceService.getDeviceById(deviceId).subscribe({
      next: (device) => {
        this.selectedDevice = device;
        this.isDetailsLoading = false;
      },
      error: () => {
        this.detailsErrorMessage = 'We could not load the selected device details.';
        this.isDetailsLoading = false;
      }
    });
  }

  private resolveSelectedDeviceId(): number {
    if (
      this.selectedDeviceId !== null &&
      this.devices.some((device) => device.id === this.selectedDeviceId)
    ) {
      return this.selectedDeviceId;
    }

    return this.devices[0].id;
  }

  private buildCreateDeviceRequest(): CreateDeviceRequest {
    const rawValue = this.createDeviceForm.getRawValue();

    return {
      name: rawValue.name.trim(),
      manufacturer: rawValue.manufacturer.trim(),
      type: rawValue.type,
      operatingSystem: rawValue.operatingSystem.trim(),
      osVersion: rawValue.osVersion.trim(),
      processor: rawValue.processor.trim(),
      ramAmountGb: rawValue.ramAmountGb,
      description: rawValue.description.trim(),
      location: rawValue.location.trim(),
      assignedUserId: null
    };
  }

  private hasDuplicateDevice(request: CreateDeviceRequest): boolean {
    const duplicateKey = this.buildDuplicateKey(request);
    return this.devices.some((device) => this.buildDuplicateKey(device) === duplicateKey);
  }

  private buildDuplicateKey(
    device: Pick<
      DeviceResponse | CreateDeviceRequest,
      'name' | 'manufacturer' | 'type' | 'operatingSystem' | 'osVersion'
    >
  ): string {
    return [
      device.name.trim().toLowerCase(),
      device.manufacturer.trim().toLowerCase(),
      device.type.toString(),
      device.operatingSystem.trim().toLowerCase(),
      device.osVersion.trim().toLowerCase()
    ].join('|');
  }

  private resetCreateDeviceForm(): void {
    this.createDeviceForm.reset({
      name: '',
      manufacturer: '',
      type: DeviceType.Phone,
      operatingSystem: '',
      osVersion: '',
      processor: '',
      ramAmountGb: 8,
      description: '',
      location: ''
    });

    this.createDeviceForm.markAsPristine();
    this.createDeviceForm.markAsUntouched();
  }
}
