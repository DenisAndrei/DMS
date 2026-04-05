import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  Validators
} from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { Device, DeviceType, UpsertDeviceRequest } from '../../core/models/device.models';
import { ProblemDetails } from '../../core/models/problem-details.model';
import { AuthSessionService } from '../../core/services/auth-session.service';
import { DeviceService } from '../../core/services/device.service';

type FormMode = 'view' | 'create' | 'edit';

type DeviceFormGroup = FormGroup<{
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

@Component({
  selector: 'app-inventory-page',
  templateUrl: './inventory-page.component.html',
  styleUrl: './inventory-page.component.css',
  standalone: false
})
export class InventoryPageComponent implements OnInit {
  private readonly api = inject(DeviceService);
  private readonly authSession = inject(AuthSessionService);
  private readonly router = inject(Router);

  readonly deviceTypes = [
    { value: 'phone' as DeviceType, label: 'Phone' },
    { value: 'tablet' as DeviceType, label: 'Tablet' }
  ];

  readonly currentUser = this.authSession.currentUser;
  readonly devices = signal<Device[]>([]);
  readonly selectedDevice = signal<Device | null>(null);
  readonly formMode = signal<FormMode>('view');
  readonly listFilter = signal('');
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly isSelecting = signal(false);
  readonly isAssigning = signal(false);
  readonly isUnassigning = signal(false);
  readonly deletingDeviceId = signal<number | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly statusMessage = signal<string | null>(null);
  readonly formSubmitted = signal(false);

  readonly filteredDevices = computed(() => {
    const query = this.normalizeText(this.listFilter());
    if (!query) {
      return this.devices();
    }

    return this.devices().filter((device) => {
      const searchableText = [
        device.name,
        device.manufacturer,
        device.operatingSystem,
        device.location,
        device.assignedUser?.name ?? ''
      ]
        .map((value) => this.normalizeText(value))
        .join(' ');

      return searchableText.includes(query);
    });
  });

  readonly totalDevices = computed(() => this.devices().length);
  readonly assignedDevices = computed(
    () => this.devices().filter((device) => device.assignedUserId !== null).length
  );
  readonly availableDevices = computed(() => this.totalDevices() - this.assignedDevices());
  readonly myAssignedDevices = computed(() => {
    const userId = this.currentUser()?.userId;
    if (!userId) {
      return 0;
    }

    return this.devices().filter((device) => device.assignedUserId === userId).length;
  });

  readonly panelEyebrow = computed(() => {
    if (this.formMode() === 'create') {
      return 'Create Device';
    }

    if (this.formMode() === 'edit') {
      return 'Update Device';
    }

    return this.selectedDevice() ? 'Selected Device' : 'Device Details';
  });

  readonly panelTitle = computed(() => {
    if (this.formMode() === 'create') {
      return 'Add a new company device';
    }

    if (this.formMode() === 'edit') {
      return this.selectedDevice()
        ? `Editing ${this.selectedDevice()!.name}`
        : 'Update the selected device';
    }

    return this.selectedDevice()
      ? `${this.selectedDevice()!.manufacturer} ${this.selectedDevice()!.name}`
      : 'Choose a device to inspect';
  });

  readonly submitButtonLabel = computed(() =>
    this.formMode() === 'create' ? 'Create device' : 'Save changes'
  );

  readonly canAssignSelectedDevice = computed(() => {
    const device = this.selectedDevice();
    return (
      this.formMode() === 'view' &&
      !!this.currentUser() &&
      !!device &&
      device.assignedUserId === null
    );
  });

  readonly canUnassignSelectedDevice = computed(() => {
    const device = this.selectedDevice();
    const userId = this.currentUser()?.userId;

    return this.formMode() === 'view' && !!device && !!userId && device.assignedUserId === userId;
  });

  readonly deviceForm: DeviceFormGroup = this.createForm();
  readonly controls = this.deviceForm.controls;

  async ngOnInit(): Promise<void> {
    await this.loadInventoryAsync();
  }

  async selectDevice(id: number): Promise<void> {
    if (!(await this.confirmDiscardChangesAsync())) {
      return;
    }

    this.errorMessage.set(null);
    this.statusMessage.set(null);
    this.isSelecting.set(true);

    try {
      const device = await firstValueFrom(this.api.getDevice(id));
      this.selectedDevice.set(device);
      this.formMode.set('view');
      this.deviceForm.markAsPristine();
      this.formSubmitted.set(false);
    } catch (error) {
      this.errorMessage.set(this.toProblemMessage(error, 'Unable to load the selected device.'));
    } finally {
      this.isSelecting.set(false);
    }
  }

  async startCreate(): Promise<void> {
    if (!(await this.confirmDiscardChangesAsync())) {
      return;
    }

    this.formMode.set('create');
    this.formSubmitted.set(false);
    this.errorMessage.set(null);
    this.statusMessage.set(null);
    this.resetForm();
  }

  startEdit(): void {
    const device = this.selectedDevice();
    if (!device) {
      return;
    }

    this.formMode.set('edit');
    this.formSubmitted.set(false);
    this.errorMessage.set(null);
    this.statusMessage.set(null);
    this.populateForm(device);
  }

  cancelEditing(): void {
    this.formMode.set('view');
    this.formSubmitted.set(false);
    this.errorMessage.set(null);
    this.populateForm(this.selectedDevice());
  }

  async saveDeviceAsync(): Promise<void> {
    const currentMode = this.formMode();
    this.formSubmitted.set(true);
    this.errorMessage.set(null);
    this.statusMessage.set(null);

    if (this.deviceForm.invalid) {
      this.deviceForm.markAllAsTouched();
      return;
    }

    const payload = this.buildRequest();
    const selectedDeviceId = this.selectedDevice()?.id ?? null;
    const duplicateDevice = this.findDuplicateDevice(
      payload,
      currentMode === 'edit' ? selectedDeviceId : null
    );

    if (duplicateDevice) {
      this.errorMessage.set(
        'A device with the same name, manufacturer, type, operating system, and OS version already exists.'
      );
      return;
    }

    this.isSaving.set(true);

    try {
      const savedDevice =
        currentMode === 'create'
          ? await firstValueFrom(this.api.createDevice(payload))
          : await firstValueFrom(this.api.updateDevice(selectedDeviceId!, payload));

      await this.refreshDevicesAsync(savedDevice.id);
      this.selectedDevice.set(savedDevice);
      this.formMode.set('view');
      this.formSubmitted.set(false);
      this.populateForm(savedDevice);
      this.statusMessage.set(
        currentMode === 'create'
          ? `${savedDevice.name} was added to the device inventory.`
          : `${savedDevice.name} was updated successfully.`
      );
    } catch (error) {
      this.errorMessage.set(
        this.toProblemMessage(
          error,
          currentMode === 'create'
            ? 'Unable to create the device.'
            : 'Unable to update the device.'
        )
      );
    } finally {
      this.isSaving.set(false);
    }
  }

  async deleteDeviceAsync(device: Device): Promise<void> {
    const confirmed = window.confirm(`Delete ${device.name} from the inventory?`);
    if (!confirmed) {
      return;
    }

    this.deletingDeviceId.set(device.id);
    this.errorMessage.set(null);
    this.statusMessage.set(null);

    try {
      await firstValueFrom(this.api.deleteDevice(device.id));

      const nextSelectedId =
        this.selectedDevice()?.id === device.id ? undefined : this.selectedDevice()?.id;

      await this.refreshDevicesAsync(nextSelectedId);

      this.formMode.set('view');
      this.formSubmitted.set(false);
      this.statusMessage.set(`${device.name} was deleted from the inventory.`);
    } catch (error) {
      this.errorMessage.set(this.toProblemMessage(error, 'Unable to delete the device.'));
    } finally {
      this.deletingDeviceId.set(null);
    }
  }

  async assignSelectedDeviceAsync(): Promise<void> {
    const device = this.selectedDevice();
    if (!device || !this.canAssignSelectedDevice()) {
      return;
    }

    this.isAssigning.set(true);
    this.errorMessage.set(null);
    this.statusMessage.set(null);

    try {
      const updatedDevice = await firstValueFrom(this.api.assignDeviceToSelf(device.id));
      await this.refreshDevicesAsync(updatedDevice.id);
      this.statusMessage.set(`${updatedDevice.name} is now assigned to you.`);
    } catch (error) {
      this.errorMessage.set(
        this.toProblemMessage(error, 'Unable to assign this device to your account.')
      );
    } finally {
      this.isAssigning.set(false);
    }
  }

  async unassignSelectedDeviceAsync(): Promise<void> {
    const device = this.selectedDevice();
    if (!device || !this.canUnassignSelectedDevice()) {
      return;
    }

    this.isUnassigning.set(true);
    this.errorMessage.set(null);
    this.statusMessage.set(null);

    try {
      const updatedDevice = await firstValueFrom(this.api.unassignDeviceFromSelf(device.id));
      await this.refreshDevicesAsync(updatedDevice.id);
      this.statusMessage.set(`${updatedDevice.name} was released from your device list.`);
    } catch (error) {
      this.errorMessage.set(
        this.toProblemMessage(error, 'Unable to unassign this device from your account.')
      );
    } finally {
      this.isUnassigning.set(false);
    }
  }

  async logoutAsync(): Promise<void> {
    this.authSession.logout();
    await this.router.navigateByUrl('/login');
  }

  updateFilter(value: string): void {
    this.listFilter.set(value);
  }

  fieldError(controlName: keyof DeviceFormGroup['controls']): string | null {
    const control = this.controls[controlName];
    if (!this.shouldShowError(control)) {
      return null;
    }

    if (control.hasError('required')) {
      return 'This field is required.';
    }

    if (control.hasError('maxlength')) {
      return `Keep this value under ${control.getError('maxlength').requiredLength} characters.`;
    }

    if (control.hasError('min') || control.hasError('max')) {
      return 'RAM must be between 1 GB and 1024 GB.';
    }

    return 'Please review this field.';
  }

  private async loadInventoryAsync(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    try {
      const devices = await firstValueFrom(this.api.getDevices());
      this.devices.set(devices);
      this.selectedDevice.set(devices[0] ?? null);
      this.populateForm(devices[0]);
    } catch (error) {
      this.errorMessage.set(this.toProblemMessage(error, 'Unable to load the device inventory.'));
      this.devices.set([]);
      this.selectedDevice.set(null);
      this.resetForm();
    } finally {
      this.isLoading.set(false);
    }
  }

  private async refreshDevicesAsync(preferredDeviceId?: number): Promise<void> {
    const devices = await firstValueFrom(this.api.getDevices());
    this.devices.set(devices);

    if (devices.length === 0) {
      this.selectedDevice.set(null);
      this.resetForm();
      return;
    }

    const currentDeviceId = preferredDeviceId ?? this.selectedDevice()?.id;
    const nextSelectedDevice = devices.find((device) => device.id === currentDeviceId) ?? devices[0];

    this.selectedDevice.set(nextSelectedDevice);
    this.populateForm(nextSelectedDevice);
  }

  private populateForm(device: Device | null | undefined): void {
    if (!device) {
      this.resetForm();
      return;
    }

    this.deviceForm.reset({
      name: device.name,
      manufacturer: device.manufacturer,
      type: device.type,
      operatingSystem: device.operatingSystem,
      osVersion: device.osVersion,
      processor: device.processor,
      ramAmountGb: device.ramAmountGb,
      description: device.description,
      location: device.location
    });

    this.deviceForm.markAsPristine();
    this.deviceForm.markAsUntouched();
  }

  private resetForm(): void {
    this.deviceForm.reset({
      name: '',
      manufacturer: '',
      type: 'phone',
      operatingSystem: '',
      osVersion: '',
      processor: '',
      ramAmountGb: 8,
      description: '',
      location: ''
    });
    this.deviceForm.markAsPristine();
    this.deviceForm.markAsUntouched();
  }

  private createForm(): DeviceFormGroup {
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

  private buildRequest(): UpsertDeviceRequest {
    const rawValue = this.deviceForm.getRawValue();

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

  private findDuplicateDevice(
    payload: UpsertDeviceRequest,
    excludedDeviceId: number | null
  ): Device | undefined {
    const payloadKey = this.createDuplicateKey(payload);

    return this.devices().find(
      (device) => device.id !== excludedDeviceId && this.createDuplicateKey(device) === payloadKey
    );
  }

  private createDuplicateKey(
    device: Pick<Device, 'name' | 'manufacturer' | 'type' | 'operatingSystem' | 'osVersion'>
  ): string {
    return [
      this.normalizeText(device.name),
      this.normalizeText(device.manufacturer),
      this.normalizeText(device.type),
      this.normalizeText(device.operatingSystem),
      this.normalizeText(device.osVersion)
    ].join('|');
  }

  private normalizeText(value: string): string {
    return value.trim().replace(/\s+/g, ' ').toLowerCase();
  }

  private shouldShowError(control: AbstractControl | null): boolean {
    return !!control && control.invalid && (control.touched || this.formSubmitted());
  }

  private toProblemMessage(error: unknown, fallbackMessage: string): string {
    if (error instanceof HttpErrorResponse) {
      const problem = error.error as ProblemDetails | null;

      if (problem?.errors) {
        const validationMessages = Object.values(problem.errors).flat();
        if (validationMessages.length > 0) {
          return validationMessages.join(' ');
        }
      }

      if (problem?.detail) {
        return problem.detail;
      }

      if (problem?.title) {
        return problem.title;
      }
    }

    return fallbackMessage;
  }

  private async confirmDiscardChangesAsync(): Promise<boolean> {
    if (this.formMode() === 'view' || !this.deviceForm.dirty) {
      return true;
    }

    return window.confirm('Discard your unsaved changes?');
  }
}
