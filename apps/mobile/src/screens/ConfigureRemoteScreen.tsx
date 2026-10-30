import React, { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  TextInput,
  ScrollView,
  SafeAreaView,
  StatusBar,
  ActivityIndicator,
} from 'react-native';
import { ChevronLeft, Bluetooth, Check, ShieldCheck, Sparkles } from 'lucide-react-native';
import { useDevices } from '../context/DeviceContext';
import { Colors } from '../theme/colors';

const ROOM_OPTIONS = ['Living Room', 'Bedroom', 'Kitchen', 'Office', 'Dining', 'Basement'];

export const ConfigureRemoteScreen: React.FC<{ route: any; navigation: any }> = ({
  route,
  navigation,
}) => {
  const { deviceType, brand } = route.params;
  const { addDevice, setActiveDevice } = useDevices();

  const [friendlyName, setFriendlyName] = useState(`${brand.name} ${deviceType.name}`);
  const [selectedRoom, setSelectedRoom] = useState('Living Room');
  const [modelName, setModelName] = useState('Series-Smart');
  const [connectionKind, setConnectionKind] = useState<'Simulation' | 'Bluetooth'>('Simulation');

  const handleSave = async () => {
    if (!friendlyName.trim()) return;

    const newDev = await addDevice({
      name: friendlyName.trim(),
      type: deviceType.id,
      brandId: brand.id,
      brandName: brand.name,
      model: modelName.trim(),
      room: selectedRoom,
      connectionKind,
    });

    setActiveDevice(newDev);
    navigation.reset({
      index: 1,
      routes: [{ name: 'Home' }, { name: 'RemoteControl', params: { deviceId: newDev.id } }],
    });
  };

  return (
    <SafeAreaView style={styles.container}>
      <StatusBar barStyle="light-content" backgroundColor="#000000" />

      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity
          style={styles.backButton}
          onPress={() => navigation.goBack()}
          activeOpacity={0.7}
        >
          <ChevronLeft size={28} color="#FFFFFF" />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>Configure Remote</Text>
        <View style={{ width: 28 }} />
      </View>

      <ScrollView contentContainerStyle={styles.scrollContent}>
        {/* Genuine Status Card */}
        <View style={styles.scanCard}>
          <View style={styles.bluetoothIconCircle}>
            <Sparkles size={36} color={Colors.primary} strokeWidth={2.2} />
          </View>
          <Text style={styles.scanTitle}>Hardware-Free Engine</Text>
          <Text style={styles.scanSubtitle}>
            Full virtual device simulation. Commands, dial controls, and states operate locally with zero external hardware required.
          </Text>
        </View>

        {/* Connection Mode Selection */}
        <Text style={styles.sectionTitle}>Connection Mode</Text>
        <TouchableOpacity
          style={[styles.modeCard, connectionKind === 'Simulation' && styles.modeCardActive]}
          onPress={() => setConnectionKind('Simulation')}
          activeOpacity={0.8}
        >
          <View style={styles.modeCardLeft}>
            <View style={styles.modeIconCircle}>
              <Sparkles size={22} color={Colors.primary} />
            </View>
            <View style={{ flex: 1 }}>
              <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
                <Text style={styles.modeTitle}>Simulation Mode</Text>
                <View style={styles.recBadge}>
                  <Text style={styles.recBadgeText}>ACTIVE</Text>
                </View>
              </View>
              <Text style={styles.modeDesc}>
                Pure simulation. Validates all inputs and logs commands without pretending physical hardware exists.
              </Text>
            </View>
          </View>
          {connectionKind === 'Simulation' && <Check size={20} color={Colors.primary} />}
        </TouchableOpacity>

        <TouchableOpacity
          style={[styles.modeCard, connectionKind === 'Bluetooth' && styles.modeCardActive]}
          onPress={() => setConnectionKind('Bluetooth')}
          activeOpacity={0.8}
        >
          <View style={styles.modeCardLeft}>
            <View style={styles.modeIconCircle}>
              <Bluetooth size={22} color="#FFFFFF" />
            </View>
            <View style={{ flex: 1 }}>
              <Text style={styles.modeTitle}>Physical Bluetooth LE</Text>
              <Text style={styles.modeDesc}>
                Requires supported physical hardware gateway and native BLE compilation permissions.
              </Text>
            </View>
          </View>
          {connectionKind === 'Bluetooth' && <Check size={20} color={Colors.primary} />}
        </TouchableOpacity>

        {/* Device Details */}
        <Text style={styles.sectionTitle}>Remote Details</Text>
        <View style={styles.inputGroup}>
          <Text style={styles.inputLabel}>Friendly Name</Text>
          <TextInput
            style={styles.textInput}
            value={friendlyName}
            onChangeText={setFriendlyName}
            placeholder="e.g. Living Room AC"
            placeholderTextColor={Colors.textMuted}
          />
        </View>

        <View style={styles.inputGroup}>
          <Text style={styles.inputLabel}>Room</Text>
          <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.roomChips}>
            {ROOM_OPTIONS.map((room) => {
              const isSelected = selectedRoom === room;
              return (
                <TouchableOpacity
                  key={room}
                  style={[styles.roomChip, isSelected && styles.roomChipSelected]}
                  onPress={() => setSelectedRoom(room)}
                  activeOpacity={0.7}
                >
                  <Text style={[styles.roomChipText, isSelected && styles.roomChipTextSelected]}>
                    {room}
                  </Text>
                </TouchableOpacity>
              );
            })}
          </ScrollView>
        </View>

        <View style={styles.inputGroup}>
          <Text style={styles.inputLabel}>Model / Profile</Text>
          <TextInput
            style={styles.textInput}
            value={modelName}
            onChangeText={setModelName}
            placeholder="e.g. Series-Smart"
            placeholderTextColor={Colors.textMuted}
          />
        </View>

        {/* Save Button */}
        <TouchableOpacity
          style={[styles.saveButton, !friendlyName.trim() && styles.saveButtonDisabled]}
          onPress={handleSave}
          disabled={!friendlyName.trim()}
          activeOpacity={0.8}
        >
          <Text style={styles.saveButtonText}>Save & Open Remote</Text>
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
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 14,
    borderBottomWidth: 1,
    borderBottomColor: '#161616',
  },
  backButton: {
    padding: 4,
    marginLeft: -4,
  },
  headerTitle: {
    fontSize: 20,
    fontWeight: '700',
    color: '#FFFFFF',
  },
  scrollContent: {
    padding: 20,
    paddingBottom: 40,
  },
  scanCard: {
    backgroundColor: '#18181A',
    borderRadius: 20,
    padding: 24,
    alignItems: 'center',
    marginBottom: 24,
    borderWidth: 1,
    borderColor: '#242428',
  },
  bluetoothIconCircle: {
    width: 72,
    height: 72,
    borderRadius: 36,
    backgroundColor: '#1F1F24',
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 16,
    borderWidth: 1,
    borderColor: '#2E2E34',
  },
  scanTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#FFFFFF',
    marginBottom: 6,
  },
  scanSubtitle: {
    fontSize: 13,
    color: Colors.textSecondary,
    textAlign: 'center',
    lineHeight: 18,
    marginBottom: 20,
    paddingHorizontal: 8,
  },
  scanButton: {
    backgroundColor: '#2C2C2E',
    paddingVertical: 12,
    paddingHorizontal: 24,
    borderRadius: 24,
    width: '100%',
    alignItems: 'center',
  },
  scanButtonActive: {
    backgroundColor: Colors.primary,
  },
  scanButtonText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#FFFFFF',
  },
  sectionTitle: {
    fontSize: 13,
    fontWeight: '700',
    color: Colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.8,
    marginBottom: 12,
  },
  modeCard: {
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
  modeCardActive: {
    borderColor: Colors.primary,
    backgroundColor: '#111D2D',
  },
  modeCardLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
    marginRight: 10,
  },
  modeIconCircle: {
    width: 44,
    height: 44,
    borderRadius: 22,
    backgroundColor: '#1C1C1E',
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 12,
  },
  modeTitle: {
    fontSize: 15,
    fontWeight: '600',
    color: '#FFFFFF',
  },
  recBadge: {
    backgroundColor: Colors.primary,
    paddingHorizontal: 6,
    paddingVertical: 1,
    borderRadius: 4,
  },
  recBadgeText: {
    fontSize: 9,
    fontWeight: '700',
    color: '#FFFFFF',
  },
  modeDesc: {
    fontSize: 12,
    color: Colors.textSecondary,
    marginTop: 2,
    lineHeight: 16,
  },
  inputGroup: {
    marginBottom: 16,
  },
  inputLabel: {
    fontSize: 13,
    fontWeight: '500',
    color: Colors.textSecondary,
    marginBottom: 6,
  },
  textInput: {
    backgroundColor: '#1C1C1E',
    borderRadius: 12,
    height: 46,
    paddingHorizontal: 14,
    fontSize: 15,
    color: '#FFFFFF',
    borderWidth: 1,
    borderColor: '#2C2C2E',
  },
  roomChips: {
    flexDirection: 'row',
  },
  roomChip: {
    backgroundColor: '#1C1C1E',
    paddingVertical: 8,
    paddingHorizontal: 14,
    borderRadius: 20,
    marginRight: 8,
    borderWidth: 1,
    borderColor: '#2C2C2E',
  },
  roomChipSelected: {
    backgroundColor: Colors.primary,
    borderColor: Colors.primary,
  },
  roomChipText: {
    fontSize: 13,
    color: Colors.textSecondary,
    fontWeight: '500',
  },
  roomChipTextSelected: {
    color: '#FFFFFF',
    fontWeight: '600',
  },
  saveButton: {
    backgroundColor: Colors.primary,
    height: 52,
    borderRadius: 16,
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 24,
  },
  saveButtonDisabled: {
    opacity: 0.5,
  },
  saveButtonText: {
    fontSize: 16,
    fontWeight: '700',
    color: '#FFFFFF',
  },
});
