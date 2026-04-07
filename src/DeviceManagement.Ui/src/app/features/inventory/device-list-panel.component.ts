import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

import { Device } from '../../core/models/device.models';

@Component({
  selector: 'app-device-list-panel',
  templateUrl: './device-list-panel.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [':host { display: block; }']
})
export class DeviceListPanelComponent {
  @Input() devices: Device[] = [];
  @Input() isLoading = false;
  @Input() hasSearchQuery = false;
  @Input() totalDeviceCount = 0;
  @Input() selectedDeviceId: number | null = null;
  @Input() currentUserId: number | null = null;
  @Input() deletingDeviceId: number | null = null;

  @Output() readonly selectRequested = new EventEmitter<number>();
  @Output() readonly deleteRequested = new EventEmitter<Device>();
}
