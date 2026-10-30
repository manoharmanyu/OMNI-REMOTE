import React from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  ScrollView,
  SafeAreaView,
  StatusBar,
} from 'react-native';
import {
  ChevronLeft,
  Power,
  Snowflake,
  Sun,
  Fan as FanIcon,
  Droplets,
  Wind,
  Volume2,
  VolumeX,
  ChevronUp,
  ChevronDown,
  Home,
  RotateCcw,
  Menu,
  Info,
  History,
  Activity,
  Sparkles,
} from 'lucide-react-native';
import { useDevices } from '../context/DeviceContext';
import { Colors } from '../theme/colors';
import { CircularDial } from '../components/CircularDial';

export const RemoteControlScreen: React.FC<{ route: any; navigation: any }> = ({
  route,
  navigation,
}) => {
  const { deviceId } = route.params;
  const { devices, sendCommand } = useDevices();

  const device = devices.find((d) => d.id === deviceId);
  if (!device) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.notFoundContainer}>
          <Text style={styles.notFoundText}>Device not found</Text>
          <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backLink}>
            <Text style={{ color: Colors.primary }}>Go Back</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  const isPowerOn = device.state.power === 'on';
  const temperature = parseInt(device.state.temperature || '24', 10);
  const currentMode = device.state.mode || 'cool';
  const currentFanSpeed = device.state.fanSpeed || 'auto';
  const isSwingOn = device.state.swing === 'true';

  const handlePowerToggle = () => {
    sendCommand(device.id, 'power.toggle');
  };

  const handleTempDelta = (delta: number) => {
    const next = Math.max(16, Math.min(30, temperature + delta));
    sendCommand(device.id, 'temperature.set', { celsius: next.toString() });
  };

  const handleModeChange = (mode: string) => {
    sendCommand(device.id, 'mode.set', { mode });
  };

  const handleSwingToggle = () => {
    sendCommand(device.id, 'swing.set', { on: isSwingOn ? 'false' : 'true' });
  };

  const handleFanSpeedChange = (speed: string) => {
    sendCommand(device.id, 'fanspeed.set', { speed });
  };

  // TV Controls
  const handleVolume = (delta: number) => {
    sendCommand(device.id, 'volume.step', { delta: delta.toString() });
  };

  const handleMute = () => {
    sendCommand(device.id, 'mute.toggle');
  };

  const handleChannel = (delta: number) => {
    sendCommand(device.id, 'channel.step', { delta: delta.toString() });
  };

  const handleNav = (key: string) => {
    sendCommand(device.id, 'nav.press', { key });
  };

  return (
    <SafeAreaView style={styles.container}>
      <StatusBar barStyle="light-content" backgroundColor="#000000" />

      {/* Top Header */}
      <View style={styles.header}>
        <TouchableOpacity
          style={styles.backButton}
          onPress={() => navigation.goBack()}
          activeOpacity={0.7}
        >
          <ChevronLeft size={28} color="#FFFFFF" />
        </TouchableOpacity>

        <View style={styles.headerCenter}>
          <Text style={styles.headerTitle}>{device.name}</Text>
          <Text style={styles.headerSubtitle}>
            {device.room || 'Living Room'} • {device.brandName} ({device.connectionKind})
          </Text>
        </View>

        <TouchableOpacity
          style={[styles.powerHeaderButton, isPowerOn && styles.powerHeaderButtonOn]}
          onPress={handlePowerToggle}
          activeOpacity={0.7}
        >
          <Power size={22} color={isPowerOn ? '#30D158' : Colors.textMuted} />
        </TouchableOpacity>
      </View>

      <ScrollView contentContainerStyle={styles.scrollContent}>
        {/* =========================================================================
            AIR CONDITIONER INTERFACE (Matching Reference Screenshot E)
           ========================================================================= */}
        {device.type === 'AirConditioner' && (
          <View>
            {/* Circular Dial */}
            <CircularDial
              temperature={temperature}
              onIncrement={() => handleTempDelta(1)}
              onDecrement={() => handleTempDelta(-1)}
              powerOn={isPowerOn}
            />

            {/* Mode Selection Grid (3 x 2) */}
            <View style={styles.modeGrid}>
              <TouchableOpacity
                style={[
                  styles.modeButton,
                  currentMode === 'cool' && isPowerOn && styles.modeButtonActive,
                ]}
                onPress={() => handleModeChange('cool')}
                disabled={!isPowerOn}
                activeOpacity={0.7}
              >
                <Snowflake
                  size={24}
                  color={currentMode === 'cool' && isPowerOn ? Colors.primary : Colors.textSecondary}
                />
                <Text
                  style={[
                    styles.modeButtonText,
                    currentMode === 'cool' && isPowerOn && styles.modeButtonTextActive,
                  ]}
                >
                  Cool
                </Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={[
                  styles.modeButton,
                  currentMode === 'heat' && isPowerOn && styles.modeButtonActive,
                ]}
                onPress={() => handleModeChange('heat')}
                disabled={!isPowerOn}
                activeOpacity={0.7}
              >
                <Sun
                  size={24}
                  color={currentMode === 'heat' && isPowerOn ? '#FF9F0A' : Colors.textSecondary}
                />
                <Text
                  style={[
                    styles.modeButtonText,
                    currentMode === 'heat' && isPowerOn && styles.modeButtonTextActive,
                  ]}
                >
                  Heat
                </Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={[
                  styles.modeButton,
                  currentMode === 'fan' && isPowerOn && styles.modeButtonActive,
                ]}
                onPress={() => handleModeChange('fan')}
                disabled={!isPowerOn}
                activeOpacity={0.7}
              >
                <FanIcon
                  size={24}
                  color={currentMode === 'fan' && isPowerOn ? Colors.primary : Colors.textSecondary}
                />
                <Text
                  style={[
                    styles.modeButtonText,
                    currentMode === 'fan' && isPowerOn && styles.modeButtonTextActive,
                  ]}
                >
                  Fan
                </Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={[
                  styles.modeButton,
                  currentMode === 'dry' && isPowerOn && styles.modeButtonActive,
                ]}
                onPress={() => handleModeChange('dry')}
                disabled={!isPowerOn}
                activeOpacity={0.7}
              >
                <Droplets
                  size={24}
                  color={currentMode === 'dry' && isPowerOn ? '#5AC8FA' : Colors.textSecondary}
                />
                <Text
                  style={[
                    styles.modeButtonText,
                    currentMode === 'dry' && isPowerOn && styles.modeButtonTextActive,
                  ]}
                >
                  Dry
                </Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={[
                  styles.modeButton,
                  currentMode === 'auto' && isPowerOn && styles.modeButtonActive,
                ]}
                onPress={() => handleModeChange('auto')}
                disabled={!isPowerOn}
                activeOpacity={0.7}
              >
                <View style={styles.autoCircle}>
                  <Text style={styles.autoText}>A</Text>
                </View>
                <Text
                  style={[
                    styles.modeButtonText,
                    currentMode === 'auto' && isPowerOn && styles.modeButtonTextActive,
                  ]}
                >
                  Auto
                </Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={[
                  styles.modeButton,
                  isSwingOn && isPowerOn && styles.modeButtonActive,
                ]}
                onPress={handleSwingToggle}
                disabled={!isPowerOn}
                activeOpacity={0.7}
              >
                <Wind
                  size={24}
                  color={isSwingOn && isPowerOn ? Colors.primary : Colors.textSecondary}
                />
                <Text
                  style={[
                    styles.modeButtonText,
                    isSwingOn && isPowerOn && styles.modeButtonTextActive,
                  ]}
                >
                  Swing
                </Text>
              </TouchableOpacity>
            </View>

            {/* Fan Speed Picker */}
            <View style={styles.fanSpeedSection}>
              <Text style={styles.controlHeading}>Fan Speed</Text>
              <View style={styles.fanSpeedRow}>
                {['auto', 'low', 'med', 'high'].map((speed) => {
                  const isActive = currentFanSpeed === speed && isPowerOn;
                  return (
                    <TouchableOpacity
                      key={speed}
                      style={[styles.fanSpeedChip, isActive && styles.fanSpeedChipActive]}
                      onPress={() => handleFanSpeedChange(speed)}
                      disabled={!isPowerOn}
                      activeOpacity={0.7}
                    >
                      <Text
                        style={[
                          styles.fanSpeedChipText,
                          isActive && styles.fanSpeedChipTextActive,
                        ]}
                      >
                        {speed.toUpperCase()}
                      </Text>
                    </TouchableOpacity>
                  );
                })}
              </View>
            </View>

            {/* Simulated Telemetry Card */}
            <View style={styles.telemetryCard}>
              <View style={styles.telemetryHeader}>
                <Sparkles size={16} color={Colors.primary} />
                <Text style={styles.telemetryTitle}>SIMULATED DATA</Text>
                <View style={styles.statusPill}>
                  <Text style={styles.statusPillText}>CONNECTED</Text>
                </View>
              </View>

              <View style={styles.telemetryMetrics}>
                <View style={styles.metricItem}>
                  <Text style={styles.metricLabel}>Room Temp</Text>
                  <Text style={styles.metricValue}>
                    {device.state['sensor.temperature'] || '23.5'}°C
                  </Text>
                </View>
                <View style={styles.metricDivider} />
                <View style={styles.metricItem}>
                  <Text style={styles.metricLabel}>Humidity</Text>
                  <Text style={styles.metricValue}>
                    {device.state['sensor.humidity'] || '48'}%
                  </Text>
                </View>
                <View style={styles.metricDivider} />
                <View style={styles.metricItem}>
                  <Text style={styles.metricLabel}>Protocol</Text>
                  <Text style={styles.metricValue}>SIM / ORDP1</Text>
                </View>
              </View>
            </View>
          </View>
        )}

        {/* =========================================================================
            TV / SET-TOP BOX / TV BOX INTERFACE
           ========================================================================= */}
        {(device.type === 'Tv' || device.type === 'SetTopBox' || device.type === 'TvBox') && (
          <View style={styles.tvControls}>
            {/* Vol / Ch Columns */}
            <View style={styles.stepperRow}>
              {/* Volume Column */}
              <View style={styles.stepperColumn}>
                <TouchableOpacity
                  style={styles.stepBtn}
                  onPress={() => handleVolume(1)}
                  disabled={!isPowerOn}
                  activeOpacity={0.7}
                >
                  <ChevronUp size={24} color="#FFFFFF" />
                </TouchableOpacity>
                <Text style={styles.stepperLabel}>VOL</Text>
                <Text style={styles.stepperValue}>{device.state.volume || '20'}</Text>
                <TouchableOpacity
                  style={styles.stepBtn}
                  onPress={() => handleVolume(-1)}
                  disabled={!isPowerOn}
                  activeOpacity={0.7}
                >
                  <ChevronDown size={24} color="#FFFFFF" />
                </TouchableOpacity>
              </View>

              {/* Mute Center Button */}
              <TouchableOpacity
                style={[
                  styles.muteBtn,
                  device.state.muted === 'true' && styles.muteBtnActive,
                ]}
                onPress={handleMute}
                disabled={!isPowerOn}
                activeOpacity={0.7}
              >
                {device.state.muted === 'true' ? (
                  <VolumeX size={26} color="#FF453A" />
                ) : (
                  <Volume2 size={26} color="#FFFFFF" />
                )}
              </TouchableOpacity>

              {/* Channel Column */}
              <View style={styles.stepperColumn}>
                <TouchableOpacity
                  style={styles.stepBtn}
                  onPress={() => handleChannel(1)}
                  disabled={!isPowerOn}
                  activeOpacity={0.7}
                >
                  <ChevronUp size={24} color="#FFFFFF" />
                </TouchableOpacity>
                <Text style={styles.stepperLabel}>CH</Text>
                <Text style={styles.stepperValue}>{device.state.channel || '1'}</Text>
                <TouchableOpacity
                  style={styles.stepBtn}
                  onPress={() => handleChannel(-1)}
                  disabled={!isPowerOn}
                  activeOpacity={0.7}
                >
                  <ChevronDown size={24} color="#FFFFFF" />
                </TouchableOpacity>
              </View>
            </View>

            {/* D-Pad Navigation */}
            <View style={styles.dpadContainer}>
              <TouchableOpacity
                style={[styles.dpadBtn, styles.dpadUp]}
                onPress={() => handleNav('up')}
                disabled={!isPowerOn}
                activeOpacity={0.7}
              >
                <ChevronUp size={28} color="#FFFFFF" />
              </TouchableOpacity>

              <View style={styles.dpadMiddleRow}>
                <TouchableOpacity
                  style={[styles.dpadBtn, styles.dpadLeft]}
                  onPress={() => handleNav('left')}
                  disabled={!isPowerOn}
                  activeOpacity={0.7}
                >
                  <ChevronLeft size={28} color="#FFFFFF" />
                </TouchableOpacity>

                <TouchableOpacity
                  style={styles.dpadCenter}
                  onPress={() => handleNav('ok')}
                  disabled={!isPowerOn}
                  activeOpacity={0.7}
                >
                  <Text style={styles.dpadOkText}>OK</Text>
                </TouchableOpacity>

                <TouchableOpacity
                  style={[styles.dpadBtn, styles.dpadRight]}
                  onPress={() => handleNav('right')}
                  disabled={!isPowerOn}
                  activeOpacity={0.7}
                >
                  <ChevronLeft
                    size={28}
                    color="#FFFFFF"
                    style={{ transform: [{ rotate: '180deg' }] }}
                  />
                </TouchableOpacity>
              </View>

              <TouchableOpacity
                style={[styles.dpadBtn, styles.dpadDown]}
                onPress={() => handleNav('down')}
                disabled={!isPowerOn}
                activeOpacity={0.7}
              >
                <ChevronDown size={28} color="#FFFFFF" />
              </TouchableOpacity>
            </View>

            {/* Function Keys Row */}
            <View style={styles.funcKeysRow}>
              <TouchableOpacity
                style={styles.funcKey}
                onPress={() => handleNav('back')}
                disabled={!isPowerOn}
              >
                <RotateCcw size={20} color="#FFFFFF" />
                <Text style={styles.funcKeyText}>Back</Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.funcKey}
                onPress={() => handleNav('home')}
                disabled={!isPowerOn}
              >
                <Home size={20} color="#FFFFFF" />
                <Text style={styles.funcKeyText}>Home</Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.funcKey}
                onPress={() => handleNav('menu')}
                disabled={!isPowerOn}
              >
                <Menu size={20} color="#FFFFFF" />
                <Text style={styles.funcKeyText}>Menu</Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.funcKey}
                onPress={() => handleNav('info')}
                disabled={!isPowerOn}
              >
                <Info size={20} color="#FFFFFF" />
                <Text style={styles.funcKeyText}>Info</Text>
              </TouchableOpacity>
            </View>
          </View>
        )}

        {/* View Activity History Button */}
        <TouchableOpacity
          style={styles.historyButton}
          onPress={() => navigation.navigate('History', { deviceId: device.id })}
          activeOpacity={0.7}
        >
          <History size={18} color={Colors.primary} style={{ marginRight: 8 }} />
          <Text style={styles.historyButtonText}>View Command Audit History</Text>
        </TouchableOpacity>
      </ScrollView>
    </SafeAreaView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#000000',
  },
  notFoundContainer: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  notFoundText: {
    fontSize: 18,
    color: '#FFFFFF',
    marginBottom: 12,
  },
  backLink: {
    padding: 8,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#161616',
  },
  backButton: {
    padding: 4,
    marginLeft: -4,
  },
  headerCenter: {
    flex: 1,
    alignItems: 'center',
    paddingHorizontal: 12,
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#FFFFFF',
  },
  headerSubtitle: {
    fontSize: 12,
    color: Colors.textSecondary,
    marginTop: 2,
  },
  powerHeaderButton: {
    width: 42,
    height: 42,
    borderRadius: 21,
    backgroundColor: '#1C1C1E',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: '#2C2C2E',
  },
  powerHeaderButtonOn: {
    backgroundColor: '#142C1D',
    borderColor: '#215732',
  },
  scrollContent: {
    padding: 16,
    paddingBottom: 40,
  },
  modeGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'space-between',
    marginTop: 12,
    gap: 12,
  },
  modeButton: {
    width: '30%',
    height: 80,
    backgroundColor: '#18181A',
    borderRadius: 16,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: '#242426',
  },
  modeButtonActive: {
    borderColor: Colors.primary,
    backgroundColor: '#111D2D',
  },
  modeButtonText: {
    fontSize: 12,
    fontWeight: '500',
    color: Colors.textSecondary,
    marginTop: 6,
  },
  modeButtonTextActive: {
    color: '#FFFFFF',
    fontWeight: '600',
  },
  autoCircle: {
    width: 26,
    height: 26,
    borderRadius: 13,
    borderWidth: 2,
    borderColor: Colors.textSecondary,
    alignItems: 'center',
    justifyContent: 'center',
  },
  autoText: {
    fontSize: 13,
    fontWeight: '700',
    color: Colors.textSecondary,
  },
  fanSpeedSection: {
    marginTop: 24,
  },
  controlHeading: {
    fontSize: 13,
    fontWeight: '700',
    color: Colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.8,
    marginBottom: 10,
  },
  fanSpeedRow: {
    flexDirection: 'row',
    backgroundColor: '#18181A',
    borderRadius: 14,
    padding: 4,
    borderWidth: 1,
    borderColor: '#242428',
  },
  fanSpeedChip: {
    flex: 1,
    paddingVertical: 10,
    alignItems: 'center',
    borderRadius: 10,
  },
  fanSpeedChipActive: {
    backgroundColor: Colors.primary,
  },
  fanSpeedChipText: {
    fontSize: 12,
    fontWeight: '600',
    color: Colors.textSecondary,
  },
  fanSpeedChipTextActive: {
    color: '#FFFFFF',
  },
  telemetryCard: {
    backgroundColor: '#141416',
    borderRadius: 16,
    padding: 16,
    marginTop: 24,
    borderWidth: 1,
    borderColor: '#202024',
  },
  telemetryHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 14,
  },
  telemetryTitle: {
    fontSize: 11,
    fontWeight: '700',
    color: Colors.primaryLight,
    letterSpacing: 1,
    marginLeft: 6,
    flex: 1,
  },
  statusPill: {
    backgroundColor: '#152E20',
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 6,
  },
  statusPillText: {
    fontSize: 10,
    fontWeight: '700',
    color: '#30D158',
  },
  telemetryMetrics: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-around',
  },
  metricItem: {
    alignItems: 'center',
  },
  metricLabel: {
    fontSize: 11,
    color: Colors.textSecondary,
    marginBottom: 4,
  },
  metricValue: {
    fontSize: 16,
    fontWeight: '700',
    color: '#FFFFFF',
  },
  metricDivider: {
    width: 1,
    height: 28,
    backgroundColor: '#26262A',
  },
  tvControls: {
    marginTop: 8,
  },
  stepperRow: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    alignItems: 'center',
    marginBottom: 28,
  },
  stepperColumn: {
    backgroundColor: '#18181A',
    borderRadius: 20,
    paddingVertical: 8,
    paddingHorizontal: 12,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#242428',
  },
  stepBtn: {
    padding: 10,
  },
  stepperLabel: {
    fontSize: 11,
    fontWeight: '700',
    color: Colors.textMuted,
    marginTop: 4,
  },
  stepperValue: {
    fontSize: 18,
    fontWeight: '700',
    color: '#FFFFFF',
    marginVertical: 4,
  },
  muteBtn: {
    width: 58,
    height: 58,
    borderRadius: 29,
    backgroundColor: '#1C1C1E',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: '#2C2C2E',
  },
  muteBtnActive: {
    backgroundColor: '#2D1616',
    borderColor: '#572121',
  },
  dpadContainer: {
    alignItems: 'center',
    justifyContent: 'center',
    marginVertical: 16,
  },
  dpadMiddleRow: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  dpadBtn: {
    width: 68,
    height: 68,
    backgroundColor: '#18181A',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: '#242428',
  },
  dpadUp: {
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
  },
  dpadDown: {
    borderBottomLeftRadius: 24,
    borderBottomRightRadius: 24,
  },
  dpadLeft: {
    borderTopLeftRadius: 24,
    borderBottomLeftRadius: 24,
  },
  dpadRight: {
    borderTopRightRadius: 24,
    borderBottomRightRadius: 24,
  },
  dpadCenter: {
    width: 80,
    height: 80,
    backgroundColor: '#222226',
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: 40,
    margin: 4,
    borderWidth: 2,
    borderColor: Colors.primary,
  },
  dpadOkText: {
    fontSize: 18,
    fontWeight: '800',
    color: '#FFFFFF',
  },
  funcKeysRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: 20,
    gap: 8,
  },
  funcKey: {
    flex: 1,
    backgroundColor: '#18181A',
    borderRadius: 14,
    paddingVertical: 12,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#242428',
  },
  funcKeyText: {
    fontSize: 11,
    color: Colors.textSecondary,
    marginTop: 4,
    fontWeight: '500',
  },
  historyButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#141C28',
    borderRadius: 14,
    paddingVertical: 14,
    marginTop: 28,
    borderWidth: 1,
    borderColor: '#1D2F4A',
  },
  historyButtonText: {
    fontSize: 14,
    fontWeight: '600',
    color: Colors.primaryLight,
  },
});
