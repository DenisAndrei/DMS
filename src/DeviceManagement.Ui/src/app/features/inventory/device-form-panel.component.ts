import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { AbstractControl, FormGroup } from '@angular/forms';

import { DeviceType } from '../../core/models/device.models';

@Component({
  selector: 'app-device-form-panel',
  templateUrl: './device-form-panel.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [':host { display: block; }']
})
export class DeviceFormPanelComponent {
  @Input({ required: true }) formGroup!: FormGroup;
  @Input() deviceTypes: ReadonlyArray<{ value: DeviceType; label: string }> = [];
  @Input() formSubmitted = false;
  @Input() isSaving = false;
  @Input() isGeneratingDescription = false;
  @Input() descriptionAssistMessage: string | null = null;
  @Input() descriptionAssistError: string | null = null;
  @Input() submitButtonLabel = 'Save changes';

  @Output() readonly saveRequested = new EventEmitter<void>();
  @Output() readonly generateDescriptionRequested = new EventEmitter<void>();

  fieldError(controlName: string): string | null {
    const control = this.formGroup.get(controlName);
    if (!control || !this.shouldShowError(control)) {
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

  private shouldShowError(control: AbstractControl): boolean {
    return control.invalid && (control.touched || this.formSubmitted);
  }
}
