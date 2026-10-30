export interface DeviceTypeItem {
  id: string;
  name: string;
  iconName: string;
  category: number;
}

export const DEVICE_TYPES: DeviceTypeItem[] = [
  { id: 'AirConditioner', name: 'Air conditioner', iconName: 'Wind', category: 1 },
  { id: 'SetTopBox', name: 'Set-top box', iconName: 'Layers', category: 2 },
  { id: 'Tv', name: 'TV', iconName: 'Tv', category: 3 },
  { id: 'TvBox', name: 'TV box', iconName: 'HardDrive', category: 4 },
  { id: 'Projector', name: 'Projector', iconName: 'Projector', category: 5 },
  { id: 'Fan', name: 'Fan', iconName: 'Fan', category: 6 },
  { id: 'LightBulb', name: 'Light bulb', iconName: 'Lightbulb', category: 7 },
  { id: 'AirPurifier', name: 'Air purifier', iconName: 'Activity', category: 8 },
  { id: 'PowerAmplifier', name: 'Power amplifier', iconName: 'Sliders', category: 9 },
  { id: 'DvdPlayer', name: 'DVD player', iconName: 'Disc', category: 10 },
  { id: 'WaterHeater', name: 'Water heater', iconName: 'Flame', category: 11 },
  { id: 'Camera', name: 'Camera', iconName: 'Camera', category: 12 },
];
