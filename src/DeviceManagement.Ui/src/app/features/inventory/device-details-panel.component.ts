import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

import { AuthenticatedUser } from '../../core/models/auth.models';
import { Device } from '../../core/models/device.models';

@Component({
  selector: 'app-device-details-panel',
  templateUrl: './device-details-panel.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [':host { display: block; }']
})
export class DeviceDetailsPanelComponent {
  @Input() device: Device | null = null;
  @Input() currentUser: AuthenticatedUser | null = null;
  @Input() canAssign = false;
  @Input() canUnassign = false;
  @Input() isAssigning = false;
  @Input() isUnassigning = false;

  @Output() readonly assignRequested = new EventEmitter<void>();
  @Output() readonly unassignRequested = new EventEmitter<void>();
}
