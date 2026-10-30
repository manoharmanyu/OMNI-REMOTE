import React, { useEffect } from 'react';
import { View, Text, StyleSheet, TouchableOpacity } from 'react-native';
import { Radio } from 'lucide-react-native';
import { Colors } from '../theme/colors';

export const SplashScreen: React.FC<{ navigation: any }> = ({ navigation }) => {
  useEffect(() => {
    const timer = setTimeout(() => {
      navigation.replace('Home');
    }, 1200);
    return () => clearTimeout(timer);
  }, [navigation]);

  return (
    <TouchableOpacity
      style={styles.container}
      activeOpacity={1}
      onPress={() => navigation.replace('Home')}
    >
      <View style={styles.centerContent}>
        <View style={styles.iconCircle}>
          <Radio size={56} color="#FFFFFF" strokeWidth={2.2} />
        </View>
        <Text style={styles.title}>OmniRemote</Text>
        <Text style={styles.tagline}>One app. Every connection. Total control.</Text>
      </View>

      <View style={styles.footer}>
        <Text style={styles.footerBadge}>SIMULATION FIRST • HARDWARE FREE</Text>
      </View>
    </TouchableOpacity>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#000000',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  centerContent: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  iconCircle: {
    width: 96,
    height: 96,
    borderRadius: 48,
    backgroundColor: '#1C1C1E',
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 20,
    borderWidth: 1,
    borderColor: '#2C2C2E',
  },
  title: {
    fontSize: 34,
    fontWeight: '800',
    color: '#FFFFFF',
    letterSpacing: -0.5,
    marginBottom: 8,
  },
  tagline: {
    fontSize: 15,
    color: Colors.textSecondary,
    textAlign: 'center',
  },
  footer: {
    position: 'absolute',
    bottom: 40,
  },
  footerBadge: {
    fontSize: 11,
    color: Colors.textMuted,
    fontWeight: '600',
    letterSpacing: 1.2,
  },
});
