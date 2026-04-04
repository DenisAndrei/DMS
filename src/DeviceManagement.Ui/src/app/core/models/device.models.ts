export enum DeviceType {
  Phone = 0,
  Tablet = 1
}

export interface UserSummary {
  id: number;
  name: string;
  role: string;
  location: string;
}

export interface DeviceResponse {
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

export interface DeviceUpsertRequest {
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

export type CreateDeviceRequest = DeviceUpsertRequest;
export type UpdateDeviceRequest = DeviceUpsertRequest;

export const DEVICE_TYPE_OPTIONS = [
  { value: DeviceType.Phone, label: 'Phone' },
  { value: DeviceType.Tablet, label: 'Tablet' }
] as const;

export function getDeviceTypeLabel(type: DeviceType): string {
  const matchingType = DEVICE_TYPE_OPTIONS.find((option) => option.value === type);
  return matchingType?.label ?? 'Unknown';
}
