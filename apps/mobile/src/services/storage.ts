import AsyncStorage from '@react-native-async-storage/async-storage';
import { RemoteDevice, ActivityLogItem } from './simulationEngine';

const DEVICES_KEY = '@omniremote_devices';
const HISTORY_KEY = '@omniremote_history';
const SETTINGS_KEY = '@omniremote_settings';

export interface AppSettings {
  serverUrl: string;
  enableCloudSync: boolean;
  simulationDelayMs: number;
}

const DEFAULT_SETTINGS: AppSettings = {
  serverUrl: 'http://127.0.0.1:5123',
  enableCloudSync: false,
  simulationDelayMs: 80,
};

export async function loadDevices(): Promise<RemoteDevice[]> {
  try {
    const raw = await AsyncStorage.getItem(DEVICES_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch {
    return [];
  }
}

export async function saveDevices(devices: RemoteDevice[]): Promise<void> {
  try {
    await AsyncStorage.setItem(DEVICES_KEY, JSON.stringify(devices));
  } catch (err) {
    console.error('Failed to save devices', err);
  }
}

export async function loadHistory(): Promise<ActivityLogItem[]> {
  try {
    const raw = await AsyncStorage.getItem(HISTORY_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch {
    return [];
  }
}

export async function saveHistory(history: ActivityLogItem[]): Promise<void> {
  try {
    // Keep last 100 entries
    const trimmed = history.slice(0, 100);
    await AsyncStorage.setItem(HISTORY_KEY, JSON.stringify(trimmed));
  } catch (err) {
    console.error('Failed to save history', err);
  }
}

export async function loadSettings(): Promise<AppSettings> {
  try {
    const raw = await AsyncStorage.getItem(SETTINGS_KEY);
    return raw ? { ...DEFAULT_SETTINGS, ...JSON.parse(raw) } : DEFAULT_SETTINGS;
  } catch {
    return DEFAULT_SETTINGS;
  }
}

export async function saveSettings(settings: AppSettings): Promise<void> {
  try {
    await AsyncStorage.setItem(SETTINGS_KEY, JSON.stringify(settings));
  } catch (err) {
    console.error('Failed to save settings', err);
  }
}
