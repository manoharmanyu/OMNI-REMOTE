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
  Alert,
} from 'react-native';
import {
  ChevronLeft,
  ShieldCheck,
  Server,
  Sliders,
  CheckCircle2,
  Trash2,
} from 'lucide-react-native';
import { useDevices } from '../context/DeviceContext';
import { Colors } from '../theme/colors';

const SECURITY_CONTROLS = [
  'Zero Hardcoded Secrets in Git',
  'PBKDF2 Password Hashing (100k iterations)',
  'Server-Side JWT Tenant Claim Isolation',
  'Strict RFC 1918 Private LAN / HTTPS SSRF Policy',
  'HMAC-SHA256 Authenticated Bluetooth Framing (ORDP/1)',
  'Idempotent Command Deduplication & Replay Guard',
  'Hardware-Free Offline Simulation Mode',
];

export const SettingsScreen: React.FC<{ navigation: any }> = ({ navigation }) => {
  const { settings, updateSettings, devices, removeDevice } = useDevices();
  const [serverUrl, setServerUrl] = useState(settings.serverUrl);
  const [delayMs, setDelayMs] = useState(settings.simulationDelayMs.toString());
  const [saved, setSaved] = useState(false);

  const handleSaveSettings = async () => {
    await updateSettings({
      ...settings,
      serverUrl: serverUrl.trim(),
      simulationDelayMs: parseInt(delayMs || '0', 10),
    });
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  };

  const handleClearAll = async () => {
    for (const d of devices) {
      await removeDevice(d.id);
    }
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
        <Text style={styles.headerTitle}>Settings & Security</Text>
        <View style={{ width: 28 }} />
      </View>

      <ScrollView contentContainerStyle={styles.scrollContent}>
        {/* Backend Endpoint */}
        <View style={styles.card}>
          <View style={styles.cardHeader}>
            <Server size={20} color={Colors.primary} style={{ marginRight: 8 }} />
            <Text style={styles.cardTitle}>Backend API Endpoint</Text>
          </View>
          <Text style={styles.cardSubtitle}>
            Connects to OmniRemote ASP.NET Core API when available.
          </Text>
          <TextInput
            style={styles.input}
            value={serverUrl}
            onChangeText={setServerUrl}
            placeholder="http://127.0.0.1:5123"
            placeholderTextColor={Colors.textMuted}
            autoCapitalize="none"
          />
        </View>

        {/* Simulation Scenario Configuration */}
        <View style={styles.card}>
          <View style={styles.cardHeader}>
            <Sliders size={20} color={Colors.primary} style={{ marginRight: 8 }} />
            <Text style={styles.cardTitle}>Simulation Engine Scenarios</Text>
          </View>
          <Text style={styles.cardSubtitle}>
            Simulate realistic command transmission latency (milliseconds).
          </Text>
          <View style={styles.delayRow}>
            {['0', '80', '250', '500'].map((val) => {
              const isSelected = delayMs === val;
              return (
                <TouchableOpacity
                  key={val}
                  style={[styles.delayChip, isSelected && styles.delayChipSelected]}
                  onPress={() => setDelayMs(val)}
                  activeOpacity={0.7}
                >
                  <Text style={[styles.delayChipText, isSelected && styles.delayChipTextSelected]}>
                    {val}ms
                  </Text>
                </TouchableOpacity>
              );
            })}
          </View>
        </View>

        {/* Save Button */}
        <TouchableOpacity
          style={styles.saveButton}
          onPress={handleSaveSettings}
          activeOpacity={0.8}
        >
          <Text style={styles.saveButtonText}>
            {saved ? '✓ Settings Saved' : 'Save Configuration'}
          </Text>
        </TouchableOpacity>

        {/* Security Controls Audit Matrix */}
        <View style={styles.card}>
          <View style={styles.cardHeader}>
            <ShieldCheck size={20} color="#30D158" style={{ marginRight: 8 }} />
            <Text style={styles.cardTitle}>Active Security Controls</Text>
          </View>
          {SECURITY_CONTROLS.map((ctrl, idx) => (
            <View key={idx} style={styles.controlRow}>
              <CheckCircle2 size={16} color="#30D158" style={{ marginRight: 8 }} />
              <Text style={styles.controlText}>{ctrl}</Text>
            </View>
          ))}
        </View>

        {/* Reset Remotes */}
        {devices.length > 0 && (
          <TouchableOpacity
            style={styles.resetButton}
            onPress={handleClearAll}
            activeOpacity={0.7}
          >
            <Trash2 size={18} color="#FF453A" style={{ marginRight: 8 }} />
            <Text style={styles.resetButtonText}>Clear All Virtual Remotes</Text>
          </TouchableOpacity>
        )}
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
    fontSize: 18,
    fontWeight: '700',
    color: '#FFFFFF',
  },
  scrollContent: {
    padding: 20,
    paddingBottom: 40,
  },
  card: {
    backgroundColor: '#18181A',
    borderRadius: 16,
    padding: 16,
    marginBottom: 16,
    borderWidth: 1,
    borderColor: '#242428',
  },
  cardHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 6,
  },
  cardTitle: {
    fontSize: 15,
    fontWeight: '700',
    color: '#FFFFFF',
  },
  cardSubtitle: {
    fontSize: 12,
    color: Colors.textSecondary,
    marginBottom: 12,
    lineHeight: 16,
  },
  input: {
    backgroundColor: '#121214',
    borderRadius: 10,
    height: 44,
    paddingHorizontal: 12,
    fontSize: 14,
    color: '#FFFFFF',
    borderWidth: 1,
    borderColor: '#2C2C2E',
  },
  delayRow: {
    flexDirection: 'row',
    gap: 8,
  },
  delayChip: {
    flex: 1,
    backgroundColor: '#121214',
    paddingVertical: 10,
    borderRadius: 10,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#2C2C2E',
  },
  delayChipSelected: {
    backgroundColor: Colors.primary,
    borderColor: Colors.primary,
  },
  delayChipText: {
    fontSize: 13,
    color: Colors.textSecondary,
    fontWeight: '500',
  },
  delayChipTextSelected: {
    color: '#FFFFFF',
    fontWeight: '700',
  },
  saveButton: {
    backgroundColor: Colors.primary,
    height: 48,
    borderRadius: 14,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 20,
  },
  saveButtonText: {
    fontSize: 15,
    fontWeight: '700',
    color: '#FFFFFF',
  },
  controlRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 7,
    borderBottomWidth: 1,
    borderBottomColor: '#1F1F22',
  },
  controlText: {
    fontSize: 13,
    color: '#D1D1D6',
    flex: 1,
  },
  resetButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#261214',
    borderRadius: 14,
    paddingVertical: 14,
    borderWidth: 1,
    borderColor: '#4A1D20',
  },
  resetButtonText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#FF453A',
  },
});
