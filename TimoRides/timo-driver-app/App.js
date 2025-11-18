import React, { useEffect } from 'react';
import { Provider } from 'react-redux';
import { PaperProvider } from 'react-native-paper';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { StatusBar } from 'expo-status-bar';
import { store } from './src/store/store';
import AppNavigator from './src/navigation/AppNavigator';
import { timoTheme } from './src/utils/theme';
import { checkAuth } from './src/store/slices/authSlice';

export default function App() {
  useEffect(() => {
    // Check authentication status on app start
    store.dispatch(checkAuth());
  }, []);

  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <Provider store={store}>
        <PaperProvider theme={timoTheme}>
          <StatusBar style="auto" />
          <AppNavigator />
        </PaperProvider>
      </Provider>
    </GestureHandlerRootView>
  );
}

