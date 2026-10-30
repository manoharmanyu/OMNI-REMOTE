export interface RemoteDevice {
  id: string;
  name: string;
  type: string;
  brandId: string;
  brandName: string;
  model: string;
  room: string;
  connectionKind: 'Simulation' | 'Bluetooth' | 'WiFi' | 'Infrared' | 'SmartHomeHub';
  state: Record<string, string>;
  createdAt: string;
}

export interface ActivityLogItem {
  id: string;
  deviceId: string;
  deviceName: string;
  commandName: string;
  parameters: string;
  status: 'Confirmed' | 'Failed' | 'Sent';
  simulated: boolean;
  timestamp: string;
  message?: string;
}

export function getDefaultStateForType(type: string): Record<string, string> {
  switch (type) {
    case 'AirConditioner':
      return {
        power: 'on',
        temperature: '24',
        mode: 'cool',
        fanSpeed: 'auto',
        swing: 'false',
        'sensor.temperature': '23.5',
        'sensor.humidity': '48',
      };
    case 'Tv':
    case 'SetTopBox':
    case 'TvBox':
      return {
        power: 'on',
        volume: '20',
        muted: 'false',
        channel: '1',
        lastKey: 'ok',
        input: 'HDMI 1',
      };
    case 'Fan':
      return {
        power: 'on',
        fanSpeed: 'medium',
        oscillation: 'false',
      };
    case 'LightBulb':
      return {
        power: 'on',
        brightness: '80',
        color: '#0A84FF',
      };
    case 'Projector':
      return {
        power: 'on',
        input: 'HDMI 1',
        aspectRatio: '16:9',
      };
    case 'AirPurifier':
      return {
        power: 'on',
        mode: 'auto',
        fanSpeed: 'medium',
        airQualityIndex: '28',
      };
    case 'PowerAmplifier':
      return {
        power: 'on',
        volume: '35',
        muted: 'false',
        input: 'AUX',
      };
    case 'DvdPlayer':
      return {
        power: 'on',
        playback: 'stopped',
        disc: 'loaded',
      };
    case 'WaterHeater':
      return {
        power: 'on',
        temperature: '48',
        ecoMode: 'true',
      };
    case 'Camera':
      return {
        power: 'on',
        privacy: 'false',
        recording: 'false',
        motionDetection: 'true',
      };
    default:
      return { power: 'on' };
  }
}

export function executeSimulatedCommand(
  device: RemoteDevice,
  commandName: string,
  params: Record<string, string> = {}
): { updatedState: Record<string, string>; activity: ActivityLogItem } {
  const state = { ...device.state };
  let status: 'Confirmed' | 'Failed' | 'Sent' = 'Confirmed';
  let message = 'Simulated command executed successfully.';

  switch (commandName) {
    case 'power.set':
      state.power = params.on === 'true' ? 'on' : 'off';
      break;
    case 'power.toggle':
      state.power = state.power === 'on' ? 'off' : 'on';
      break;
    case 'temperature.set':
      if (params.celsius) state.temperature = params.celsius;
      break;
    case 'mode.set':
      if (params.mode) state.mode = params.mode;
      break;
    case 'fanspeed.set':
      if (params.speed) state.fanSpeed = params.speed;
      break;
    case 'swing.set':
      state.swing = params.on === 'true' ? 'true' : 'false';
      break;
    case 'volume.step': {
      const delta = parseInt(params.delta || '0', 10);
      const curr = parseInt(state.volume || '15', 10);
      state.volume = Math.max(0, Math.min(100, curr + delta)).toString();
      break;
    }
    case 'mute.toggle':
      state.muted = state.muted === 'true' ? 'false' : 'true';
      break;
    case 'channel.step': {
      const delta = parseInt(params.delta || '1', 10);
      const curr = parseInt(state.channel || '1', 10);
      state.channel = Math.max(1, curr + delta).toString();
      break;
    }
    case 'channel.set':
      if (params.number) state.channel = params.number;
      break;
    case 'nav.press':
      if (params.key) state.lastKey = params.key;
      break;
    case 'brightness.set':
      if (params.percent) state.brightness = params.percent;
      break;
    case 'oscillation.set':
      state.oscillation = params.on === 'true' ? 'true' : 'false';
      break;
    default:
      // Merge params directly
      Object.assign(state, params);
      break;
  }

  // Slight natural sensor fluctuation
  if (state['sensor.temperature']) {
    const base = parseFloat(state.temperature || '24');
    const jitter = (Math.random() * 0.4 - 0.2).toFixed(1);
    state['sensor.temperature'] = (base - 0.5 + parseFloat(jitter)).toFixed(1);
  }

  const paramStr = Object.entries(params)
    .map(([k, v]) => `${k}=${v}`)
    .join(', ');

  const activity: ActivityLogItem = {
    id: Math.random().toString(36).substring(2, 10),
    deviceId: device.id,
    deviceName: device.name,
    commandName,
    parameters: paramStr || 'none',
    status,
    simulated: true,
    timestamp: new Date().toISOString(),
    message,
  };

  return { updatedState: state, activity };
}
