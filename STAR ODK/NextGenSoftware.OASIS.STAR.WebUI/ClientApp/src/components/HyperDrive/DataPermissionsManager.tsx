import React, { useState, useEffect } from 'react';
import { 
  DataPermissions, 
  AvatarPermissions, 
  HolonPermissions, 
  ProviderPermissions,
  FieldLevelPermissions,
  AccessControl,
  AvatarFieldPermission,
  HolonTypePermission,
  HolonFieldPermission,
  ProviderPermission,
  FieldPermissionRule,
  AccessPolicy,
  PermissionLevel,
  AuthorizationLevel,
  EncryptionLevel
} from '../../types/hyperDriveTypes';
import { hyperDriveService } from '../../services/core/hyperDriveService';

interface DataPermissionsManagerProps {
  onPermissionsUpdate: (permissions: DataPermissions) => void;
}

const DataPermissionsManager: React.FC<DataPermissionsManagerProps> = ({ onPermissionsUpdate }) => {
  const [permissions, setPermissions] = useState<DataPermissions | null>(null);
  const [activeTab, setActiveTab] = useState('avatar');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Sample avatar fields
  const avatarFields = [
    { name: 'Id', description: 'Unique identifier', isSensitive: false },
    { name: 'Name', description: 'Display name', isSensitive: false },
    { name: 'Email', description: 'Email address', isSensitive: true },
    { name: 'Username', description: 'Username', isSensitive: false },
    { name: 'Avatar', description: 'Avatar image', isSensitive: false },
    { name: 'Karma', description: 'Karma points', isSensitive: false },
    { name: 'XP', description: 'Experience points', isSensitive: false },
    { name: 'Level', description: 'User level', isSensitive: false },
    { name: 'DOB', description: 'Date of birth', isSensitive: true },
    { name: 'Address', description: 'Physical address', isSensitive: true },
    { name: 'Phone', description: 'Phone number', isSensitive: true },
    { name: 'Bio', description: 'Biography', isSensitive: false },
    { name: 'Interests', description: 'User interests', isSensitive: false },
    { name: 'Skills', description: 'User skills', isSensitive: false },
    { name: 'Preferences', description: 'User preferences', isSensitive: true }
  ];

  // Sample holon types
  const holonTypes = [
    { name: 'Avatar', description: 'User avatar data', isSensitive: true },
    { name: 'Holon', description: 'Generic holon data', isSensitive: false },
    { name: 'Quest', description: 'Quest information', isSensitive: false },
    { name: 'Mission', description: 'Mission data', isSensitive: false },
    { name: 'NFT', description: 'NFT metadata', isSensitive: false },
    { name: 'Karma', description: 'Karma data', isSensitive: false },
    { name: 'Inventory', description: 'User inventory', isSensitive: true },
    { name: 'Wallet', description: 'Wallet information', isSensitive: true },
    { name: 'Profile', description: 'User profile', isSensitive: true },
    { name: 'Settings', description: 'User settings', isSensitive: true }
  ];

  // Sample providers
  const providers = [
    { type: 'MongoOASIS', name: 'MongoDB', isFree: true, category: 'storage' },
    { type: 'IPFSOASIS', name: 'IPFS', isFree: true, category: 'storage' },
    { type: 'EthereumOASIS', name: 'Ethereum', isFree: false, category: 'blockchain' },
    { type: 'PolygonOASIS', name: 'Polygon', isFree: false, category: 'blockchain' },
    { type: 'SEEDSOASIS', name: 'SEEDS', isFree: true, category: 'network' },
    { type: 'HoloOASIS', name: 'Holo', isFree: true, category: 'network' }
  ];

  useEffect(() => {
    loadPermissions();
  }, []);

  const loadPermissions = async () => {
    setLoading(true);
    try {
      const data = await hyperDriveService.getDataPermissions();
      setPermissions(data);
    } catch (err) {
      setError('Failed to load permissions');
      console.error('Error loading permissions:', err);
    } finally {
      setLoading(false);
    }
  };

  const updateAvatarFieldPermission = (fieldName: string, providerType: string, permission: PermissionLevel, isEncrypted: boolean) => {
    if (!permissions) return;

    const updatedPermissions = { ...permissions };
    const fieldPermission = updatedPermissions.avatarPermissions.fields.find(f => f.fieldName === fieldName);
    
    if (fieldPermission) {
      fieldPermission.permission = permission;
      fieldPermission.isEncrypted = isEncrypted;
    } else {
      updatedPermissions.avatarPermissions.fields.push({
        fieldName,
        permission,
        isEncrypted,
        isRequired: false,
        providerTypes: [providerType]
      });
    }

    setPermissions(updatedPermissions);
    onPermissionsUpdate(updatedPermissions);
  };

  const updateHolonTypePermission = (holonType: string, providerType: string, permission: PermissionLevel, isEncrypted: boolean) => {
    if (!permissions) return;

    const updatedPermissions = { ...permissions };
    const holonPermission = updatedPermissions.holonPermissions.holonTypes.find(h => h.holonType === holonType);
    
    if (holonPermission) {
      holonPermission.permission = permission;
      holonPermission.isEncrypted = isEncrypted;
    } else {
      updatedPermissions.holonPermissions.holonTypes.push({
        holonType,
        permission,
        isEncrypted,
        isRequired: false,
        providerTypes: [providerType],
        fields: []
      });
    }

    setPermissions(updatedPermissions);
    onPermissionsUpdate(updatedPermissions);
  };

  const renderAvatarPermissions = () => (
    <div className="space-y-6">
      <div className="bg-white p-6 rounded-lg shadow">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">👤 Avatar Field Permissions</h3>
        
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Field
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Description
                </th>
                {providers.map(provider => (
                  <th key={provider.type} className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    <div className="flex items-center space-x-2">
                      <span>{provider.name}</span>
                      {provider.isFree && <span className="text-green-600">🆓</span>}
                    </div>
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {avatarFields.map(field => (
                <tr key={field.name}>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center">
                      <span className="text-sm font-medium text-gray-900">{field.name}</span>
                      {field.isSensitive && <span className="ml-2 text-red-500">🔒</span>}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {field.description}
                  </td>
                  {providers.map(provider => (
                    <td key={provider.type} className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center space-x-2">
                        <select
                          value={permissions?.avatarPermissions.fields.find(f => f.fieldName === field.name)?.permission || PermissionLevel.Read}
                          onChange={(e) => updateAvatarFieldPermission(
                            field.name, 
                            provider.type, 
                            e.target.value as PermissionLevel,
                            permissions?.avatarPermissions.fields.find(f => f.fieldName === field.name)?.isEncrypted || false
                          )}
                          className="text-sm border border-gray-300 rounded-md px-2 py-1"
                        >
                          <option value={PermissionLevel.None}>None</option>
                          <option value={PermissionLevel.Read}>Read</option>
                          <option value={PermissionLevel.Write}>Write</option>
                          <option value={PermissionLevel.Admin}>Admin</option>
                          <option value={PermissionLevel.Owner}>Owner</option>
                        </select>
                        <input
                          type="checkbox"
                          checked={permissions?.avatarPermissions.fields.find(f => f.fieldName === field.name)?.isEncrypted || false}
                          onChange={(e) => updateAvatarFieldPermission(
                            field.name, 
                            provider.type, 
                            permissions?.avatarPermissions.fields.find(f => f.fieldName === field.name)?.permission || PermissionLevel.Read,
                            e.target.checked
                          )}
                          className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                        />
                        <span className="text-xs text-gray-500">🔐</span>
                      </div>
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );

  const renderHolonPermissions = () => (
    <div className="space-y-6">
      <div className="bg-white p-6 rounded-lg shadow">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">🧩 Holon Type Permissions</h3>
        
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Holon Type
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Description
                </th>
                {providers.map(provider => (
                  <th key={provider.type} className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    <div className="flex items-center space-x-2">
                      <span>{provider.name}</span>
                      {provider.isFree && <span className="text-green-600">🆓</span>}
                    </div>
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {holonTypes.map(holonType => (
                <tr key={holonType.name}>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center">
                      <span className="text-sm font-medium text-gray-900">{holonType.name}</span>
                      {holonType.isSensitive && <span className="ml-2 text-red-500">🔒</span>}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {holonType.description}
                  </td>
                  {providers.map(provider => (
                    <td key={provider.type} className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center space-x-2">
                        <select
                          value={permissions?.holonPermissions.holonTypes.find(h => h.holonType === holonType.name)?.permission || PermissionLevel.Read}
                          onChange={(e) => updateHolonTypePermission(
                            holonType.name, 
                            provider.type, 
                            e.target.value as PermissionLevel,
                            permissions?.holonPermissions.holonTypes.find(h => h.holonType === holonType.name)?.isEncrypted || false
                          )}
                          className="text-sm border border-gray-300 rounded-md px-2 py-1"
                        >
                          <option value={PermissionLevel.None}>None</option>
                          <option value={PermissionLevel.Read}>Read</option>
                          <option value={PermissionLevel.Write}>Write</option>
                          <option value={PermissionLevel.Admin}>Admin</option>
                          <option value={PermissionLevel.Owner}>Owner</option>
                        </select>
                        <input
                          type="checkbox"
                          checked={permissions?.holonPermissions.holonTypes.find(h => h.holonType === holonType.name)?.isEncrypted || false}
                          onChange={(e) => updateHolonTypePermission(
                            holonType.name, 
                            provider.type, 
                            permissions?.holonPermissions.holonTypes.find(h => h.holonType === holonType.name)?.permission || PermissionLevel.Read,
                            e.target.checked
                          )}
                          className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                        />
                        <span className="text-xs text-gray-500">🔐</span>
                      </div>
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );

  const renderProviderPermissions = () => (
    <div className="space-y-6">
      <div className="bg-white p-6 rounded-lg shadow">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">🔧 Provider Permissions</h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {providers.map(provider => (
            <div key={provider.type} className="border border-gray-200 rounded-lg p-4">
              <div className="flex items-center justify-between mb-3">
                <h4 className="font-semibold text-gray-900">{provider.name}</h4>
                <span className={`px-2 py-1 text-xs rounded-full ${
                  provider.isFree ? 'bg-green-100 text-green-800' : 'bg-orange-100 text-orange-800'
                }`}>
                  {provider.isFree ? 'FREE' : 'PAID'}
                </span>
              </div>
              
              <div className="space-y-3">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Permission Level
                  </label>
                  <select className="w-full text-sm border border-gray-300 rounded-md px-2 py-1">
                    <option value={PermissionLevel.Read}>Read</option>
                    <option value={PermissionLevel.Write}>Write</option>
                    <option value={PermissionLevel.Admin}>Admin</option>
                    <option value={PermissionLevel.Owner}>Owner</option>
                  </select>
                </div>
                
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Allowed Data Types
                  </label>
                  <div className="space-y-1">
                    {['Avatar', 'Holon', 'Quest', 'NFT'].map(dataType => (
                      <label key={dataType} className="flex items-center">
                        <input type="checkbox" className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded" />
                        <span className="ml-2 text-sm text-gray-700">{dataType}</span>
                      </label>
                    ))}
                  </div>
                </div>
                
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Cost Limit ($)
                  </label>
                  <input
                    type="number"
                    step="0.01"
                    className="w-full text-sm border border-gray-300 rounded-md px-2 py-1"
                    placeholder="0.00"
                  />
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );

  const renderAccessControl = () => (
    <div className="space-y-6">
      <div className="bg-white p-6 rounded-lg shadow">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">🔐 Access Control</h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Authentication Required
            </label>
            <input
              type="checkbox"
              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
            />
          </div>
          
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Authorization Level
            </label>
            <select className="w-full border border-gray-300 rounded-md px-3 py-2">
              <option value={AuthorizationLevel.Public}>Public</option>
              <option value={AuthorizationLevel.Authenticated}>Authenticated</option>
              <option value={AuthorizationLevel.Authorized}>Authorized</option>
              <option value={AuthorizationLevel.Admin}>Admin</option>
              <option value={AuthorizationLevel.Owner}>Owner</option>
            </select>
          </div>
          
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Encryption Level
            </label>
            <select className="w-full border border-gray-300 rounded-md px-3 py-2">
              <option value={EncryptionLevel.None}>None</option>
              <option value={EncryptionLevel.Basic}>Basic</option>
              <option value={EncryptionLevel.Standard}>Standard</option>
              <option value={EncryptionLevel.High}>High</option>
              <option value={EncryptionLevel.Military}>Military</option>
            </select>
          </div>
          
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Audit Logging
            </label>
            <input
              type="checkbox"
              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
            />
          </div>
        </div>
      </div>
    </div>
  );

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-4">
        <p className="text-red-800">{error}</p>
        <button 
          onClick={loadPermissions}
          className="mt-2 px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700"
        >
          Retry
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <h2 className="text-xl font-semibold text-blue-900 mb-2">🔐 Data Permissions Management</h2>
        <p className="text-blue-800">
          Configure granular permissions for each field and data type across different providers.
          Control what data gets shared where and with what level of access.
        </p>
      </div>

      <div className="bg-white shadow rounded-lg">
        <div className="border-b border-gray-200">
          <nav className="-mb-px flex space-x-8 px-6">
            {[
              { id: 'avatar', name: 'Avatar Fields' },
              { id: 'holon', name: 'Holon Types' },
              { id: 'provider', name: 'Provider Access' },
              { id: 'access', name: 'Access Control' }
            ].map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`py-4 px-1 border-b-2 font-medium text-sm ${
                  activeTab === tab.id
                    ? 'border-blue-500 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }`}
              >
                {tab.name}
              </button>
            ))}
          </nav>
        </div>

        <div className="p-6">
          {activeTab === 'avatar' && renderAvatarPermissions()}
          {activeTab === 'holon' && renderHolonPermissions()}
          {activeTab === 'provider' && renderProviderPermissions()}
          {activeTab === 'access' && renderAccessControl()}
        </div>
      </div>
    </div>
  );
};

export default DataPermissionsManager;
