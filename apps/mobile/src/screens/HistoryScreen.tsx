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
import { ChevronLeft, Trash2, CheckCircle2, Clock } from 'lucide-react-native';
import { useDevices } from '../context/DeviceContext';
import { Colors } from '../theme/colors';

export const HistoryScreen: React.FC<{ navigation: any; route: any }> = ({
  navigation,
  route,
}) => {
  const { history, clearHistory } = useDevices();
  const filterDeviceId = route.params?.deviceId;

  const displayList = filterDeviceId
    ? history.filter((h) => h.deviceId === filterDeviceId)
    : history;

  const formatTime = (isoString: string) => {
    try {
      const d = new Date(isoString);
      return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
    } catch {
      return isoString;
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
        <Text style={styles.headerTitle}>Activity Audit Log</Text>
        {displayList.length > 0 ? (
          <TouchableOpacity
            style={styles.clearButton}
            onPress={clearHistory}
            activeOpacity={0.7}
          >
            <Trash2 size={20} color="#FF453A" />
          </TouchableOpacity>
        ) : (
          <View style={{ width: 28 }} />
        )}
      </View>

      {displayList.length === 0 ? (
        <View style={styles.emptyContainer}>
          <Clock size={48} color={Colors.textMuted} strokeWidth={1.5} />
          <Text style={styles.emptyTitle}>No activity yet</Text>
          <Text style={styles.emptySubtitle}>
            Commands sent to your virtual remotes will appear here with verification telemetry.
          </Text>
        </View>
      ) : (
        <FlatList
          data={displayList}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.listContent}
          renderItem={({ item }) => (
            <View style={styles.historyCard}>
              <View style={styles.cardHeader}>
                <View style={styles.cardHeaderLeft}>
                  <CheckCircle2 size={16} color="#30D158" style={{ marginRight: 6 }} />
                  <Text style={styles.commandName}>{item.commandName}</Text>
                </View>
                <Text style={styles.timestamp}>{formatTime(item.timestamp)}</Text>
              </View>

              <Text style={styles.deviceName}>{item.deviceName}</Text>

              <View style={styles.paramsBox}>
                <Text style={styles.paramsText}>{item.parameters}</Text>
              </View>

              <View style={styles.footerRow}>
                <View style={styles.simBadge}>
                  <Text style={styles.simBadgeText}>SIMULATION</Text>
                </View>
                <Text style={styles.statusText}>Status: {item.status}</Text>
              </View>
            </View>
          )}
        />
      )}
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
  clearButton: {
    padding: 6,
  },
  emptyContainer: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 32,
  },
  emptyTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#FFFFFF',
    marginTop: 16,
    marginBottom: 8,
  },
  emptySubtitle: {
    fontSize: 13,
    color: Colors.textSecondary,
    textAlign: 'center',
    lineHeight: 18,
  },
  listContent: {
    padding: 16,
  },
  historyCard: {
    backgroundColor: '#18181A',
    borderRadius: 14,
    padding: 14,
    marginBottom: 10,
    borderWidth: 1,
    borderColor: '#242428',
  },
  cardHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: 4,
  },
  cardHeaderLeft: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  commandName: {
    fontSize: 15,
    fontWeight: '700',
    color: '#FFFFFF',
  },
  timestamp: {
    fontSize: 12,
    color: Colors.textMuted,
  },
  deviceName: {
    fontSize: 13,
    color: Colors.textSecondary,
    marginBottom: 8,
  },
  paramsBox: {
    backgroundColor: '#101012',
    padding: 8,
    borderRadius: 8,
    marginBottom: 10,
  },
  paramsText: {
    fontSize: 12,
    fontFamily: 'Courier',
    color: Colors.primaryLight,
  },
  footerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  simBadge: {
    backgroundColor: '#14253D',
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 4,
  },
  simBadgeText: {
    fontSize: 9,
    fontWeight: '700',
    color: Colors.primaryLight,
  },
  statusText: {
    fontSize: 12,
    color: '#30D158',
    fontWeight: '600',
  },
});
