import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateDeviceRequest,
  DeviceResponse,
  UpdateDeviceRequest
} from '../models/device.models';

@Injectable({
  providedIn: 'root'
})
export class DeviceService {
  private readonly httpClient = inject(HttpClient);
  private readonly devicesEndpoint = '/api/devices';

  getDevices(): Observable<DeviceResponse[]> {
    return this.httpClient.get<DeviceResponse[]>(this.devicesEndpoint);
  }

  getDeviceById(deviceId: number): Observable<DeviceResponse> {
    return this.httpClient.get<DeviceResponse>(`${this.devicesEndpoint}/${deviceId}`);
  }

  createDevice(request: CreateDeviceRequest): Observable<DeviceResponse> {
    return this.httpClient.post<DeviceResponse>(this.devicesEndpoint, request);
  }

  updateDevice(deviceId: number, request: UpdateDeviceRequest): Observable<DeviceResponse> {
    return this.httpClient.put<DeviceResponse>(`${this.devicesEndpoint}/${deviceId}`, request);
  }

  deleteDevice(deviceId: number): Observable<void> {
    return this.httpClient.delete<void>(`${this.devicesEndpoint}/${deviceId}`);
  }
}
