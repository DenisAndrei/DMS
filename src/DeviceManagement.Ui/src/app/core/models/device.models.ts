export type DeviceType = 'phone' | 'tablet';

export interface UserSummary {
  id: number;
  name: string;
  role: string;
  location: string;
}

export interface Device {
  id: number;
  name: string;
  manufacturer: string;
  type: DeviceType;
  operatingSystem: string;
  osVersion: string;
  processor: string;
  ramAmountGb: number;
  description: string;
  location: string;
  assignedUserId: number | null;
  assignedUser: UserSummary | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface UpsertDeviceRequest {
  name: string;
  manufacturer: string;
  type: DeviceType;
  operatingSystem: string;
  osVersion: string;
  processor: string;
  ramAmountGb: number;
  description: string;
  location: string;
  assignedUserId: number | null;
}

export type CreateDeviceRequest = UpsertDeviceRequest;
export type UpdateDeviceRequest = UpsertDeviceRequest;

export interface GenerateDeviceDescriptionRequest {
  name: string;
  manufacturer: string;
  type: DeviceType;
  operatingSystem: string;
  osVersion: string;
  processor: string;
  ramAmountGb: number;
}

export interface GenerateDeviceDescriptionResponse {
  description: string;
  provider: string;
  model: string;
  usedFallback: boolean;
}

export const DEVICE_TYPE_OPTIONS = [
  { value: 'phone' as DeviceType, label: 'Phone' },
  { value: 'tablet' as DeviceType, label: 'Tablet' }
] as const;

export function getDeviceTypeLabel(type: DeviceType): string {
  const matchingType = DEVICE_TYPE_OPTIONS.find((option) => option.value === type);
  return matchingType?.label ?? 'Unknown';
}
