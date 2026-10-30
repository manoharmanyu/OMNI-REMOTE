import React, { createContext, useContext, useState, useEffect } from 'react';
import { RemoteDevice, ActivityLogItem, executeSimulatedCommand, getDefaultStateForType } from '../services/simulationEngine';
import { loadDevices, saveDevices, loadHistory, saveHistory, loadSettings, saveSettings, AppSettings } from '../services/storage';

interface DeviceContextType {
  devices: RemoteDevice[];
  activeDevice: RemoteDevice | null;
  history: ActivityLogItem[];
  settings: AppSettings;
  isLoading: boolean;
  setActiveDevice: (device: RemoteDevice | null) => void;
  addDevice: (newDevice: Omit<RemoteDevice, 'id' | 'state' | 'createdAt'>) => Promise<RemoteDevice>;
  removeDevice: (id: string) => Promise<void>;
  sendCommand: (deviceId: string, commandName: string, params?: Record<string, string>) => Promise<void>;
  clearHistory: () => Promise<void>;
  updateSettings: (newSettings: AppSettings) => Promise<void>;
}

const DeviceContext = createContext<DeviceContextType | undefined>(undefined);

export const DeviceProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [devices, setDevices] = useState<RemoteDevice[]>([]);
  const [activeDevice, setActiveDevice] = useState<RemoteDevice | null>(null);
  const [history, setHistory] = useState<ActivityLogItem[]>([]);
  const [settings, setSettingsState] = useState<AppSettings>({
    serverUrl: 'http://127.0.0.1:5123',
    enableCloudSync: false,
    simulationDelayMs: 80,
  });
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function init() {
      const [storedDevices, storedHistory, storedSettings] = await Promise.all([
        loadDevices(),
        loadHistory(),
        loadSettings(),
      ]);

      // If first launch and no devices, add a sample Carrier Air Conditioner for instant testing!
      if (storedDevices.length === 0) {
        const sampleAC: RemoteDevice = {
          id: 'demo-carrier-ac',
          name: 'Living Room AC',
          type: 'AirConditioner',
          brandId: 'carrier',
          brandName: 'Carrier',
          model: 'Infinity-21',
          room: 'Living Room',
          connectionKind: 'Simulation',
          state: getDefaultStateForType('AirConditioner'),
          createdAt: new Date().toISOString(),
        };
        storedDevices.push(sampleAC);
        await saveDevices(storedDevices);
      }

      setDevices(storedDevices);
      setHistory(storedHistory);
      setSettingsState(storedSettings);
      setIsLoading(false);
    }
    init();
  }, []);

  const addDevice = async (newDeviceData: Omit<RemoteDevice, 'id' | 'state' | 'createdAt'>): Promise<RemoteDevice> => {
    const id = Math.random().toString(36).substring(2, 9) + '-' + Date.now().toString(36);
    const newDevice: RemoteDevice = {
      ...newDeviceData,
      id,
      state: getDefaultStateForType(newDeviceData.type),
      createdAt: new Date().toISOString(),
    };

    const updated = [newDevice, ...devices];
    setDevices(updated);
    await saveDevices(updated);
    return newDevice;
  };

  const removeDevice = async (id: string) => {
    const updated = devices.filter((d) => d.id !== id);
    setDevices(updated);
    if (activeDevice?.id === id) setActiveDevice(null);
    await saveDevices(updated);
  };

  const sendCommand = async (deviceId: string, commandName: string, params: Record<string, string> = {}) => {
    const dev = devices.find((d) => d.id === deviceId);
    if (!dev) return;

    if (settings.simulationDelayMs > 0) {
      await new Promise((res) => setTimeout(res, settings.simulationDelayMs));
    }

    const { updatedState, activity } = executeSimulatedCommand(dev, commandName, params);

    const updatedDevices = devices.map((d) => (d.id === deviceId ? { ...d, state: updatedState } : d));
    setDevices(updatedDevices);

    if (activeDevice?.id === deviceId) {
      setActiveDevice({ ...dev, state: updatedState });
    }

    const updatedHistory = [activity, ...history];
    setHistory(updatedHistory);
    await Promise.all([saveDevices(updatedDevices), saveHistory(updatedHistory)]);
  };

  const clearHistory = async () => {
    setHistory([]);
    await saveHistory([]);
  };

  const updateSettings = async (newSettings: AppSettings) => {
    setSettingsState(newSettings);
    await saveSettings(newSettings);
  };

  return (
    <DeviceContext.Provider
      value={{
        devices,
        activeDevice,
        history,
        settings,
        isLoading,
        setActiveDevice,
        addDevice,
        removeDevice,
        sendCommand,
        clearHistory,
        updateSettings,
      }}
    >
      {children}
    </DeviceContext.Provider>
  );
};

export const useDevices = (): DeviceContextType => {
  const context = useContext(DeviceContext);
  if (!context) throw new Error('useDevices must be used within a DeviceProvider');
  return context;
};
