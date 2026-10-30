import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';

import { SplashScreen } from '../screens/SplashScreen';
import { HomeScreen } from '../screens/HomeScreen';
import { SelectTypeScreen } from '../screens/SelectTypeScreen';
import { SelectBrandScreen } from '../screens/SelectBrandScreen';
import { ConfigureRemoteScreen } from '../screens/ConfigureRemoteScreen';
import { RemoteControlScreen } from '../screens/RemoteControlScreen';
import { HistoryScreen } from '../screens/HistoryScreen';
import { SettingsScreen } from '../screens/SettingsScreen';

const Stack = createNativeStackNavigator();

export const AppNavigator: React.FC = () => {
  return (
    <NavigationContainer>
      <Stack.Navigator
        initialRouteName="Splash"
        screenOptions={{
          headerShown: false,
          contentStyle: { backgroundColor: '#000000' },
          animation: 'fade_from_bottom',
        }}
      >
        <Stack.Screen name="Splash" component={SplashScreen} />
        <Stack.Screen name="Home" component={HomeScreen} />
        <Stack.Screen name="SelectType" component={SelectTypeScreen} />
        <Stack.Screen name="SelectBrand" component={SelectBrandScreen} />
        <Stack.Screen name="ConfigureRemote" component={ConfigureRemoteScreen} />
        <Stack.Screen name="RemoteControl" component={RemoteControlScreen} />
        <Stack.Screen name="History" component={HistoryScreen} />
        <Stack.Screen name="Settings" component={SettingsScreen} />
      </Stack.Navigator>
    </NavigationContainer>
  );
};
