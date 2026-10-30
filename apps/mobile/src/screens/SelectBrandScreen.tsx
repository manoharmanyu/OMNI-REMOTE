import React, { useState, useMemo } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  TextInput,
  SectionList,
  SafeAreaView,
  StatusBar,
} from 'react-native';
import { ChevronLeft, Search, X } from 'lucide-react-native';
import { DeviceTypeItem } from '../data/deviceTypes';
import { Colors } from '../theme/colors';
import brandsData from '../data/brands.json';

interface BrandItem {
  id: string;
  name: string;
  secondary?: string;
  aliases?: string[];
  types: string[];
}

const ALPHABET = ['#', ...'ABCDEFGHIJKLMNOPQRSTUVWXYZ'.split('')];

export const SelectBrandScreen: React.FC<{ route: any; navigation: any }> = ({
  route,
  navigation,
}) => {
  const { deviceType } = route.params as { deviceType: DeviceTypeItem };
  const [query, setQuery] = useState('');

  // Filter brands compatible with this device type
  const relevantBrands: BrandItem[] = useMemo(() => {
    return (brandsData.brands as BrandItem[]).filter((b) =>
      b.types.includes(deviceType.id)
    );
  }, [deviceType.id]);

  // Search filter
  const filteredBrands = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return relevantBrands;
    return relevantBrands.filter(
      (b) =>
        b.name.toLowerCase().includes(q) ||
        (b.secondary && b.secondary.toLowerCase().includes(q)) ||
        (b.aliases && b.aliases.some((a) => a.toLowerCase().includes(q)))
    );
  }, [relevantBrands, query]);

  // Top brands list (top 8 known brands for this type)
  const topBrands = useMemo(() => {
    if (query) return [];
    return relevantBrands.slice(0, 8);
  }, [relevantBrands, query]);

  // Group into Alphabetical Sections
  const sections = useMemo(() => {
    const map = new Map<string, BrandItem[]>();

    filteredBrands.forEach((brand) => {
      const firstChar = brand.name[0].toUpperCase();
      const letter = /[A-Z]/.test(firstChar) ? firstChar : '#';
      if (!map.has(letter)) {
        map.set(letter, []);
      }
      map.get(letter)!.push(brand);
    });

    const result = Array.from(map.entries())
      .sort(([a], [b]) => {
        if (a === '#') return -1;
        if (b === '#') return 1;
        return a.localeCompare(b);
      })
      .map(([letter, data]) => ({
        title: letter,
        data: data.sort((a, b) => a.name.localeCompare(b.name)),
      }));

    return result;
  }, [filteredBrands]);

  const handleSelectBrand = (brand: BrandItem) => {
    navigation.navigate('ConfigureRemote', {
      deviceType,
      brand,
    });
  };

  return (
    <SafeAreaView style={styles.container}>
      <StatusBar barStyle="light-content" backgroundColor="#000000" />

      {/* Top Header / Search */}
      <View style={styles.header}>
        <TouchableOpacity
          style={styles.backButton}
          onPress={() => navigation.goBack()}
          activeOpacity={0.7}
        >
          <ChevronLeft size={28} color="#FFFFFF" />
        </TouchableOpacity>

        <View style={styles.searchBar}>
          <Search size={18} color={Colors.textSecondary} style={{ marginRight: 8 }} />
          <TextInput
            style={styles.searchInput}
            placeholder={`Search for brand ${deviceType.name}`}
            placeholderTextColor={Colors.textMuted}
            value={query}
            onChangeText={setQuery}
            autoCapitalize="none"
          />
          {query.length > 0 && (
            <TouchableOpacity onPress={() => setQuery('')} hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}>
              <X size={16} color={Colors.textSecondary} />
            </TouchableOpacity>
          )}
        </View>
      </View>

      {/* Brand List */}
      <View style={styles.listContainer}>
        <SectionList
          sections={sections}
          keyExtractor={(item) => item.id}
          ListHeaderComponent={
            topBrands.length > 0 ? (
              <View style={styles.topBrandsSection}>
                <Text style={styles.sectionHeading}>Top brands</Text>
                {topBrands.map((brand) => (
                  <TouchableOpacity
                    key={'top-' + brand.id}
                    style={styles.brandRow}
                    onPress={() => handleSelectBrand(brand)}
                    activeOpacity={0.7}
                  >
                    <View>
                      <Text style={styles.brandName}>{brand.name}</Text>
                      {brand.secondary && (
                        <Text style={styles.brandSecondary}>{brand.secondary}</Text>
                      )}
                    </View>
                  </TouchableOpacity>
                ))}
              </View>
            ) : null
          }
          renderSectionHeader={({ section: { title } }) => (
            <View style={styles.letterHeader}>
              <Text style={styles.letterHeaderText}>{title}</Text>
            </View>
          )}
          renderItem={({ item }) => (
            <TouchableOpacity
              style={styles.brandRow}
              onPress={() => handleSelectBrand(item)}
              activeOpacity={0.7}
            >
              <View>
                <Text style={styles.brandName}>{item.name}</Text>
                {item.secondary && (
                  <Text style={styles.brandSecondary}>{item.secondary}</Text>
                )}
              </View>
            </TouchableOpacity>
          )}
        />

        {/* Right Alphabet Index */}
        <View style={styles.alphabetIndex}>
          {ALPHABET.map((letter) => (
            <Text key={letter} style={styles.alphabetLetter}>
              {letter}
            </Text>
          ))}
        </View>
      </View>
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
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#161616',
    gap: 8,
  },
  backButton: {
    padding: 4,
    marginLeft: -4,
  },
  searchBar: {
    flex: 1,
    height: 42,
    backgroundColor: '#1C1C1E',
    borderRadius: 12,
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    borderWidth: 1,
    borderColor: '#2C2C2E',
  },
  searchInput: {
    flex: 1,
    fontSize: 14,
    color: '#FFFFFF',
    paddingVertical: 0,
  },
  listContainer: {
    flex: 1,
    flexDirection: 'row',
  },
  topBrandsSection: {
    paddingBottom: 8,
  },
  sectionHeading: {
    fontSize: 13,
    fontWeight: '700',
    color: Colors.textMuted,
    paddingHorizontal: 20,
    paddingTop: 16,
    paddingBottom: 8,
    textTransform: 'uppercase',
    letterSpacing: 0.8,
  },
  letterHeader: {
    backgroundColor: '#121214',
    paddingHorizontal: 20,
    paddingVertical: 6,
  },
  letterHeaderText: {
    fontSize: 13,
    fontWeight: '700',
    color: Colors.primaryLight,
  },
  brandRow: {
    paddingHorizontal: 20,
    paddingVertical: 14,
    borderBottomWidth: 1,
    borderBottomColor: '#18181A',
  },
  brandName: {
    fontSize: 16,
    fontWeight: '500',
    color: '#FFFFFF',
  },
  brandSecondary: {
    fontSize: 12,
    color: Colors.textSecondary,
    marginTop: 2,
  },
  alphabetIndex: {
    width: 24,
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 8,
  },
  alphabetLetter: {
    fontSize: 9,
    fontWeight: '600',
    color: Colors.textMuted,
    paddingVertical: 1,
  },
});
