import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity } from 'react-native';
import { Minus, Plus } from 'lucide-react-native';
import { Colors } from '../theme/colors';

interface CircularDialProps {
  temperature: number;
  minTemp?: number;
  maxTemp?: number;
  onIncrement: () => void;
  onDecrement: () => void;
  powerOn: boolean;
}

export const CircularDial: React.FC<CircularDialProps> = ({
  temperature,
  minTemp = 16,
  maxTemp = 30,
  onIncrement,
  onDecrement,
  powerOn,
}) => {
  // Calculate dial angle percentage
  const fraction = Math.max(0, Math.min(1, (temperature - minTemp) / (maxTemp - minTemp)));

  return (
    <View style={styles.container}>
      {/* Minus Button */}
      <TouchableOpacity
        style={[styles.stepButton, !powerOn && styles.buttonDisabled]}
        onPress={onDecrement}
        disabled={!powerOn || temperature <= minTemp}
        activeOpacity={0.7}
      >
        <Minus size={28} color={powerOn && temperature > minTemp ? Colors.text : Colors.textMuted} />
      </TouchableOpacity>

      {/* Central Circular Display */}
      <View style={[styles.outerRing, !powerOn && styles.ringDimmed]}>
        <View style={styles.innerRing}>
          <Text style={[styles.tempNumber, !powerOn && styles.textDimmed]}>
            {powerOn ? temperature : '--'}
          </Text>
          <Text style={[styles.tempUnit, !powerOn && styles.textDimmed]}>
            °C
          </Text>
        </View>

        {/* Glow indicator arc overlay */}
        <View style={[styles.accentArc, { opacity: powerOn ? 0.9 : 0.2 }]} />
      </View>

      {/* Plus Button */}
      <TouchableOpacity
        style={[styles.stepButton, !powerOn && styles.buttonDisabled]}
        onPress={onIncrement}
        disabled={!powerOn || temperature >= maxTemp}
        activeOpacity={0.7}
      >
        <Plus size={28} color={powerOn && temperature < maxTemp ? Colors.text : Colors.textMuted} />
      </TouchableOpacity>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    marginVertical: 24,
    paddingHorizontal: 16,
  },
  stepButton: {
    width: 52,
    height: 52,
    borderRadius: 26,
    backgroundColor: '#1C1C1E',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: '#2C2C2E',
  },
  buttonDisabled: {
    opacity: 0.4,
  },
  outerRing: {
    width: 190,
    height: 190,
    borderRadius: 95,
    borderWidth: 4,
    borderColor: '#2A2A2E',
    marginHorizontal: 20,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#0F0F11',
    shadowColor: Colors.primary,
    shadowOffset: { width: 0, height: 0 },
    shadowOpacity: 0.35,
    shadowRadius: 18,
    elevation: 8,
  },
  ringDimmed: {
    shadowOpacity: 0,
    borderColor: '#1C1C1E',
  },
  innerRing: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    justifyContent: 'center',
  },
  tempNumber: {
    fontSize: 58,
    fontWeight: '700',
    color: '#FFFFFF',
    letterSpacing: -1,
  },
  tempUnit: {
    fontSize: 22,
    fontWeight: '500',
    color: Colors.textSecondary,
    marginTop: 8,
    marginLeft: 2,
  },
  textDimmed: {
    color: Colors.textMuted,
  },
  accentArc: {
    position: 'absolute',
    top: -4,
    left: 20,
    right: 20,
    height: 4,
    backgroundColor: Colors.primary,
    borderRadius: 2,
  },
});
