import React from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  FlatList,
  SafeAreaView,
  StatusBar,
  Dimensions,
} from 'react-native';
import {
  ChevronLeft,
  Wind,
  Layers,
  Tv,
  HardDrive,
  Projector,
  Fan,
  Lightbulb,
  Activity,
  Sliders,
  Disc,
  Flame,
  Camera,
} from 'lucide-react-native';
import { DEVICE_TYPES, DeviceTypeItem } from '../data/deviceTypes';
import { Colors } from '../theme/colors';

const { width } = Dimensions.get('window');
const COLUMN_WIDTH = (width - 32 - 16) / 3;

export const SelectTypeScreen: React.FC<{ navigation: any }> = ({ navigation }) => {
  const renderIcon = (iconName: string) => {
    const size = 30;
    const color = '#FFFFFF';
    switch (iconName) {
      case 'Wind':
        return <Wind size={size} color={color} />;
      case 'Layers':
        return <Layers size={size} color={color} />;
      case 'Tv':
        return <Tv size={size} color={color} />;
      case 'HardDrive':
        return <HardDrive size={size} color={color} />;
      case 'Projector':
        return <Projector size={size} color={color} />;
      case 'Fan':
        return <Fan size={size} color={color} />;
      case 'Lightbulb':
        return <Lightbulb size={size} color={color} />;
      case 'Activity':
        return <Activity size={size} color={color} />;
      case 'Sliders':
        return <Sliders size={size} color={color} />;
      case 'Disc':
        return <Disc size={size} color={color} />;
      case 'Flame':
        return <Flame size={size} color={color} />;
      case 'Camera':
        return <Camera size={size} color={color} />;
      default:
        return <Tv size={size} color={color} />;
    }
  };

  const handleSelectType = (item: DeviceTypeItem) => {
    navigation.navigate('SelectBrand', { deviceType: item });
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
        <Text style={styles.headerTitle}>Select type</Text>
        <View style={{ width: 28 }} />
      </View>

      {/* 3-Column Grid */}
      <FlatList
        data={DEVICE_TYPES}
        keyExtractor={(item) => item.id}
        numColumns={3}
        contentContainerStyle={styles.gridContainer}
        columnWrapperStyle={styles.columnWrapper}
        renderItem={({ item }) => (
          <TouchableOpacity
            style={styles.tile}
            onPress={() => handleSelectType(item)}
            activeOpacity={0.75}
          >
            <View style={styles.iconContainer}>{renderIcon(item.iconName)}</View>
            <Text style={styles.tileLabel} numberOfLines={2}>
              {item.name}
            </Text>
          </TouchableOpacity>
        )}
      />
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
  gridContainer: {
    padding: 16,
    paddingTop: 20,
  },
  columnWrapper: {
    justifyContent: 'space-between',
    marginBottom: 12,
  },
  tile: {
    width: COLUMN_WIDTH,
    height: 110,
    backgroundColor: '#1C1C1E',
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 8,
    paddingVertical: 12,
    borderWidth: 1,
    borderColor: '#262628',
  },
  iconContainer: {
    marginBottom: 10,
    height: 36,
    alignItems: 'center',
    justifyContent: 'center',
  },
  tileLabel: {
    fontSize: 12,
    fontWeight: '500',
    color: '#E5E5E7',
    textAlign: 'center',
    lineHeight: 16,
  },
});
