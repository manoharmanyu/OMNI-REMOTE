import React from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  FlatList,
  SafeAreaView,
  StatusBar,
} from 'react-native';
import {
  Plus,
  User,
  History,
  Tv,
  Wind,
  Fan,
  Lightbulb,
  Radio,
  Power,
  Sliders,
  ChevronRight,
} from 'lucide-react-native';
import { useDevices } from '../context/DeviceContext';
import { Colors } from '../theme/colors';
import { RemoteDevice } from '../services/simulationEngine';

export const HomeScreen: React.FC<{ navigation: any }> = ({ navigation }) => {
  const { devices, setActiveDevice, sendCommand } = useDevices();

  const getDeviceIcon = (type: string, color: string = '#FFFFFF', size: number = 24) => {
    switch (type) {
      case 'AirConditioner':
        return <Wind size={size} color={color} />;
      case 'Tv':
      case 'SetTopBox':
      case 'TvBox':
        return <Tv size={size} color={color} />;
      case 'Fan':
        return <Fan size={size} color={color} />;
      case 'LightBulb':
        return <Lightbulb size={size} color={color} />;
      default:
        return <Radio size={size} color={color} />;
    }
  };

  const handleDevicePress = (device: RemoteDevice) => {
    setActiveDevice(device);
    navigation.navigate('RemoteControl', { deviceId: device.id });
  };

  const handlePowerToggle = (e: any, device: RemoteDevice) => {
    e.stopPropagation();
    sendCommand(device.id, 'power.toggle');
  };

  return (
    <SafeAreaView style={styles.container}>
      <StatusBar barStyle="light-content" backgroundColor="#000000" />

      {/* Header */}
      <View style={styles.header}>
        <Text style={styles.headerTitle}>Smart Remote</Text>
        <View style={styles.headerActions}>
          <TouchableOpacity
            style={styles.iconButton}
            onPress={() => navigation.navigate('History')}
            activeOpacity={0.7}
          >
            <History size={22} color="#FFFFFF" />
          </TouchableOpacity>
          <TouchableOpacity
            style={styles.iconButton}
            onPress={() => navigation.navigate('Settings')}
            activeOpacity={0.7}
          >
            <User size={22} color="#FFFFFF" />
          </TouchableOpacity>
        </View>
      </View>

      {/* Content */}
      {devices.length === 0 ? (
        <View style={styles.emptyContainer}>
          <View style={styles.emptyIllustration}>
            <Radio size={48} color={Colors.textMuted} strokeWidth={1.5} />
          </View>
          <Text style={styles.emptyText}>No remote controls</Text>
          <Text style={styles.emptySubtext}>Tap the + button to add your first virtual remote</Text>
        </View>
      ) : (
        <FlatList
          data={devices}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.listContent}
          renderItem={({ item }) => {
            const isPowerOn = item.state?.power === 'on';
            return (
              <TouchableOpacity
                style={styles.deviceCard}
                onPress={() => handleDevicePress(item)}
                activeOpacity={0.75}
              >
                <View style={styles.cardLeft}>
                  <View style={[styles.deviceIconBox, isPowerOn && styles.deviceIconBoxActive]}>
                    {getDeviceIcon(item.type, isPowerOn ? Colors.primary : Colors.textMuted, 26)}
                  </View>
                  <View style={styles.deviceInfo}>
                    <Text style={styles.deviceName}>{item.name}</Text>
                    <Text style={styles.deviceSubtitle}>
                      {item.room || 'General'} • {item.brandName}
                    </Text>
                    <View style={styles.badgeRow}>
                      <View style={styles.simulationBadge}>
                        <Text style={styles.simulationBadgeText}>SIMULATION</Text>
                      </View>
                      {item.type === 'AirConditioner' && isPowerOn && (
                        <Text style={styles.stateSummary}>
                          {item.state.temperature || '24'}°C • {item.state.mode || 'cool'}
                        </Text>
                      )}
                    </View>
                  </View>
                </View>

                <View style={styles.cardRight}>
                  <TouchableOpacity
                    style={[styles.powerButton, isPowerOn && styles.powerButtonActive]}
                    onPress={(e) => handlePowerToggle(e, item)}
                    activeOpacity={0.7}
                  >
                    <Power size={18} color={isPowerOn ? '#30D158' : Colors.textMuted} />
                  </TouchableOpacity>
                  <ChevronRight size={18} color={Colors.textMuted} style={{ marginLeft: 6 }} />
                </View>
              </TouchableOpacity>
            );
          }}
        />
      )}

      {/* Floating Action Button (FAB) */}
      <TouchableOpacity
        style={styles.fab}
        onPress={() => navigation.navigate('SelectType')}
        activeOpacity={0.85}
      >
        <Plus size={30} color="#FFFFFF" strokeWidth={2.5} />
      </TouchableOpacity>
    </SafeAreaView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#000000',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 20,
    paddingTop: 16,
    paddingBottom: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#161616',
  },
  headerTitle: {
    fontSize: 26,
    fontWeight: '700',
    color: '#FFFFFF',
    letterSpacing: -0.5,
  },
  headerActions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  iconButton: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: '#1C1C1E',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: '#2C2C2E',
  },
  emptyContainer: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 32,
  },
  emptyIllustration: {
    width: 90,
    height: 90,
    borderRadius: 45,
    backgroundColor: '#18181A',
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 20,
    borderWidth: 1,
    borderColor: '#242428',
  },
  emptyText: {
    fontSize: 20,
    fontWeight: '600',
    color: '#FFFFFF',
    marginBottom: 8,
  },
  emptySubtext: {
    fontSize: 14,
    color: Colors.textSecondary,
    textAlign: 'center',
    lineHeight: 20,
  },
  listContent: {
    padding: 16,
    paddingBottom: 90,
  },
  deviceCard: {
    backgroundColor: '#181818',
    borderRadius: 16,
    padding: 16,
    marginBottom: 12,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    borderWidth: 1,
    borderColor: '#242426',
  },
  cardLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  deviceIconBox: {
    width: 50,
    height: 50,
    borderRadius: 14,
    backgroundColor: '#1C1C1E',
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 14,
  },
  deviceIconBoxActive: {
    backgroundColor: '#14253D',
  },
  deviceInfo: {
    flex: 1,
  },
  deviceName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#FFFFFF',
    marginBottom: 2,
  },
  deviceSubtitle: {
    fontSize: 13,
    color: Colors.textSecondary,
    marginBottom: 6,
  },
  badgeRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  simulationBadge: {
    backgroundColor: '#1C1C1E',
    paddingHorizontal: 7,
    paddingVertical: 2,
    borderRadius: 6,
    borderWidth: 1,
    borderColor: '#2E2E32',
  },
  simulationBadgeText: {
    fontSize: 10,
    fontWeight: '700',
    color: Colors.primaryLight,
    letterSpacing: 0.5,
  },
  stateSummary: {
    fontSize: 12,
    color: Colors.textSecondary,
  },
  cardRight: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  powerButton: {
    width: 38,
    height: 38,
    borderRadius: 19,
    backgroundColor: '#1C1C1E',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: '#2C2C2E',
  },
  powerButtonActive: {
    backgroundColor: '#162E20',
    borderColor: '#215732',
  },
  fab: {
    position: 'absolute',
    bottom: 28,
    right: 24,
    width: 62,
    height: 62,
    borderRadius: 31,
    backgroundColor: Colors.primary,
    alignItems: 'center',
    justifyContent: 'center',
    shadowColor: Colors.primary,
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.4,
    shadowRadius: 10,
    elevation: 8,
  },
});
