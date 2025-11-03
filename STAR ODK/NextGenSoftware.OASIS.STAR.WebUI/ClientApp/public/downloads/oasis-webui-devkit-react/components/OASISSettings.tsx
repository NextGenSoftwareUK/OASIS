import React, { useState } from 'react';
import { useMutation } from 'react-query';
import { OASISClient } from '@oasis/api-client';

export interface OASISSettingsProps {
  avatarId: string;
  theme?: 'light' | 'dark';
  sections?: string[];
  customStyles?: React.CSSProperties;
}

export const OASISSettings: React.FC<OASISSettingsProps> = ({
  avatarId,
  theme = 'dark',
  sections = ['general', 'privacy', 'notifications', 'advanced'],
  customStyles = {}
}) => {
  const [activeSection, setActiveSection] = useState(sections[0]);
  const [settings, setSettings] = useState<any>({
    general: {
      language: 'en',
      timezone: 'UTC',
      dateFormat: 'MM/DD/YYYY'
    },
    privacy: {
      profileVisibility: 'public',
      showEmail: false,
      showKarma: true
    },
    notifications: {
      emailNotifications: true,
      pushNotifications: false,
      chatNotifications: true
    },
    advanced: {
      developerMode: false,
      debugLogs: false,
      experimentalFeatures: false
    }
  });

  const client = new OASISClient();

  const saveMutation = useMutation(
    (data: any) => client.updateSettings(avatarId, data)
  );

  const handleSave = () => {
    saveMutation.mutate(settings);
  };

  const updateSetting = (section: string, key: string, value: any) => {
    setSettings({
      ...settings,
      [section]: {
        ...settings[section],
        [key]: value
      }
    });
  };

  return (
    <div className={`oasis-settings oasis-settings--${theme}`} style={customStyles}>
      <div className="settings-sidebar">
        {sections.map(section => (
          <button
            key={section}
            className={`section-btn ${activeSection === section ? 'active' : ''}`}
            onClick={() => setActiveSection(section)}
          >
            {section.charAt(0).toUpperCase() + section.slice(1)}
          </button>
        ))}
      </div>

      <div className="settings-content">
        <h3>{activeSection.charAt(0).toUpperCase() + activeSection.slice(1)} Settings</h3>
        
        {activeSection === 'general' && (
          <div className="settings-group">
            <label>
              Language:
              <select
                value={settings.general.language}
                onChange={(e) => updateSetting('general', 'language', e.target.value)}
              >
                <option value="en">English</option>
                <option value="es">Español</option>
                <option value="fr">Français</option>
              </select>
            </label>
            <label>
              Timezone:
              <select
                value={settings.general.timezone}
                onChange={(e) => updateSetting('general', 'timezone', e.target.value)}
              >
                <option value="UTC">UTC</option>
                <option value="EST">EST</option>
                <option value="PST">PST</option>
              </select>
            </label>
          </div>
        )}

        {activeSection === 'privacy' && (
          <div className="settings-group">
            <label>
              <input
                type="checkbox"
                checked={settings.privacy.showEmail}
                onChange={(e) => updateSetting('privacy', 'showEmail', e.target.checked)}
              />
              Show Email Publicly
            </label>
            <label>
              <input
                type="checkbox"
                checked={settings.privacy.showKarma}
                onChange={(e) => updateSetting('privacy', 'showKarma', e.target.checked)}
              />
              Show Karma Score
            </label>
          </div>
        )}

        {activeSection === 'notifications' && (
          <div className="settings-group">
            <label>
              <input
                type="checkbox"
                checked={settings.notifications.emailNotifications}
                onChange={(e) => updateSetting('notifications', 'emailNotifications', e.target.checked)}
              />
              Email Notifications
            </label>
            <label>
              <input
                type="checkbox"
                checked={settings.notifications.pushNotifications}
                onChange={(e) => updateSetting('notifications', 'pushNotifications', e.target.checked)}
              />
              Push Notifications
            </label>
          </div>
        )}

        {activeSection === 'advanced' && (
          <div className="settings-group">
            <label>
              <input
                type="checkbox"
                checked={settings.advanced.developerMode}
                onChange={(e) => updateSetting('advanced', 'developerMode', e.target.checked)}
              />
              Developer Mode
            </label>
            <label>
              <input
                type="checkbox"
                checked={settings.advanced.experimentalFeatures}
                onChange={(e) => updateSetting('advanced', 'experimentalFeatures', e.target.checked)}
              />
              Experimental Features
            </label>
          </div>
        )}

        <button 
          className="save-btn"
          onClick={handleSave}
          disabled={saveMutation.isLoading}
        >
          {saveMutation.isLoading ? 'Saving...' : 'Save Settings'}
        </button>
      </div>
    </div>
  );
};

export default OASISSettings;

