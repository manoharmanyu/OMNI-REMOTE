export const Colors = {
  background: '#000000',
  card: '#181818',
  cardHover: '#242426',
  tile: '#1C1C1E',
  tileActive: '#2C2C2E',
  border: '#2C2C2E',
  borderLight: '#3A3A3C',
  divider: '#1F1F1F',
  
  text: '#FFFFFF',
  textSecondary: '#8E8E93',
  textMuted: '#636366',
  
  primary: '#0A84FF',
  primaryDark: '#0066CC',
  primaryLight: '#5AC8FA',
  
  accent: '#0A84FF',
  success: '#30D158',
  warning: '#FF9F0A',
  danger: '#FF453A',
  
  dialTrack: '#1C1C1E',
  dialProgress: '#0A84FF',
  badgeBg: '#14253D',
  badgeText: '#5AC8FA',
};

export const Typography = {
  titleLarge: { fontSize: 28, fontWeight: '700' as const, color: Colors.text },
  titleMedium: { fontSize: 20, fontWeight: '600' as const, color: Colors.text },
  titleSmall: { fontSize: 16, fontWeight: '600' as const, color: Colors.text },
  body: { fontSize: 14, fontWeight: '400' as const, color: Colors.text },
  caption: { fontSize: 12, fontWeight: '400' as const, color: Colors.textSecondary },
};
