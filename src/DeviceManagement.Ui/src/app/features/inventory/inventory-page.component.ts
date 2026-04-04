import { Component, OnInit } from '@angular/core';
import { DeviceResponse, getDeviceTypeLabel } from '../../core/models/device.models';
import { DeviceService } from '../../core/services/device.service';

@Component({
  selector: 'app-inventory-page',
  templateUrl: './inventory-page.component.html',
  styleUrl: './inventory-page.component.css',
  standalone: false
})
export class InventoryPageComponent implements OnInit {
  protected devices: DeviceResponse[] = [];
  protected isLoading = true;
  protected errorMessage = '';

  constructor(private readonly deviceService: DeviceService) {}

  ngOnInit(): void {
    this.loadDevices();
  }

  protected reloadDevices(): void {
    this.loadDevices();
  }

  protected getDeviceTypeLabel(type: DeviceResponse['type']): string {
    return getDeviceTypeLabel(type);
  }

  private loadDevices(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.deviceService.getDevices().subscribe({
      next: (devices) => {
        this.devices = [...devices].sort((left, right) => left.name.localeCompare(right.name));
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'We could not load the device inventory right now.';
        this.isLoading = false;
      }
    });
  }
}
