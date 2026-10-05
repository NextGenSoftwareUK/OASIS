import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { OASISClient } from '@oasis/api-client';

export interface DataManagementProps {
  avatarId: string;
  theme?: 'light' | 'dark';
  enableExport?: boolean;
  enableImport?: boolean;
  customStyles?: React.CSSProperties;
}

export const DataManagement: React.FC<DataManagementProps> = ({
  avatarId,
  theme = 'dark',
  enableExport = true,
  enableImport = true,
  customStyles = {}
}) => {
  const [newKey, setNewKey] = useState('');
  const [newValue, setNewValue] = useState('');
  const queryClient = useQueryClient();
  const client = new OASISClient();

  const { data: dataItems, isLoading } = useQuery(
    ['data', avatarId],
    () => client.getAvatarData(avatarId)
  );

  const saveMutation = useMutation(
    ({ key, value }: any) => client.saveData(avatarId, key, value),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['data', avatarId]);
        setNewKey('');
        setNewValue('');
      }
    }
  );

  const deleteMutation = useMutation(
    (key: string) => client.deleteData(avatarId, key),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['data', avatarId]);
      }
    }
  );

  const handleSave = () => {
    if (newKey && newValue) {
      saveMutation.mutate({ key: newKey, value: newValue });
    }
  };

  const handleExport = () => {
    const dataStr = JSON.stringify(dataItems, null, 2);
    const blob = new Blob([dataStr], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `oasis-data-${avatarId}.json`;
    link.click();
  };

  return (
    <div className={`oasis-data-mgmt oasis-data-mgmt--${theme}`} style={customStyles}>
      <div className="data-header">
        <h3>Data Management</h3>
        <div className="data-actions">
          {enableExport && (
            <button onClick={handleExport}>📥 Export</button>
          )}
          {enableImport && (
            <button>📤 Import</button>
          )}
        </div>
      </div>

      <div className="data-create">
        <input
          type="text"
          placeholder="Key"
          value={newKey}
          onChange={(e) => setNewKey(e.target.value)}
        />
        <textarea
          placeholder="Value (JSON)"
          value={newValue}
          onChange={(e) => setNewValue(e.target.value)}
          rows={3}
        />
        <button onClick={handleSave} disabled={saveMutation.isLoading}>
          {saveMutation.isLoading ? 'Saving...' : 'Save Data'}
        </button>
      </div>

      <div className="data-list">
        {isLoading ? (
          <div>Loading data...</div>
        ) : (
          Object.entries(dataItems || {}).map(([key, value]) => (
            <div key={key} className="data-item">
              <div className="data-key">{key}</div>
              <div className="data-value">
                <pre>{JSON.stringify(value, null, 2)}</pre>
              </div>
              <button
                className="delete-btn"
                onClick={() => deleteMutation.mutate(key)}
              >
                🗑️
              </button>
            </div>
          ))
        )}
      </div>
    </div>
  );
};

export default DataManagement;

