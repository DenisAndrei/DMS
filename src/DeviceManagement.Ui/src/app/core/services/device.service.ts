import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Device,
  GenerateDeviceDescriptionRequest,
  GenerateDeviceDescriptionResponse,
  UpsertDeviceRequest
} from '../models/device.models';

@Injectable({
  providedIn: 'root'
})
export class DeviceService {
  private readonly httpClient = inject(HttpClient);
  private readonly devicesEndpoint = '/api/devices';

  getDevices(): Observable<Device[]> {
    return this.httpClient.get<Device[]>(this.devicesEndpoint);
  }

  searchDevices(query: string): Observable<Device[]> {
    return this.httpClient.get<Device[]>(`${this.devicesEndpoint}/search`, {
      params: { q: query }
    });
  }

  getDevice(deviceId: number): Observable<Device> {
    return this.httpClient.get<Device>(`${this.devicesEndpoint}/${deviceId}`);
  }

  getDeviceById(deviceId: number): Observable<Device> {
    return this.getDevice(deviceId);
  }

  createDevice(request: UpsertDeviceRequest): Observable<Device> {
    return this.httpClient.post<Device>(this.devicesEndpoint, request);
  }

  generateDescription(
    request: GenerateDeviceDescriptionRequest
  ): Observable<GenerateDeviceDescriptionResponse> {
    return this.httpClient.post<GenerateDeviceDescriptionResponse>(
      `${this.devicesEndpoint}/generate-description`,
      request
    );
  }

  updateDevice(deviceId: number, request: UpsertDeviceRequest): Observable<Device> {
    return this.httpClient.put<Device>(`${this.devicesEndpoint}/${deviceId}`, request);
  }

  assignDeviceToSelf(deviceId: number): Observable<Device> {
    return this.httpClient.post<Device>(`${this.devicesEndpoint}/${deviceId}/assign`, {});
  }

  unassignDeviceFromSelf(deviceId: number): Observable<Device> {
    return this.httpClient.post<Device>(`${this.devicesEndpoint}/${deviceId}/unassign`, {});
  }

  deleteDevice(deviceId: number): Observable<void> {
    return this.httpClient.delete<void>(`${this.devicesEndpoint}/${deviceId}`);
  }
}
