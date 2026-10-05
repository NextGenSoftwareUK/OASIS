/**
 * STARNET Demo Data
 * Demo data for STARNET operations (OAPPs, Templates, Runtimes, etc.)
 */

export const starnetDemoData = {
  // OAPP Operations
  oapp: {
    create: (payload: any) => ({
      id: 'demo-oapp-1',
      name: payload.name || 'Demo OAPP',
      description: payload.description || 'A demo OASIS Application',
      type: payload.type || 'Game',
      version: '1.0.0',
      isPublished: false,
      isInstalled: false,
      createdOn: new Date().toISOString(),
      createdBy: 'demo-avatar-1',
    }),

    update: (id: string, payload: any) => ({
      id,
      ...payload,
      updatedOn: new Date().toISOString(),
    }),

    publish: (id: string) => ({
      id,
      isPublished: true,
      publishedOn: new Date().toISOString(),
      publishedBy: 'demo-avatar-1',
    }),

    unpublish: (id: string) => ({
      id,
      isPublished: false,
      unpublishedOn: new Date().toISOString(),
    }),

    download: (id: string, destinationPath: string) => ({
      id,
      path: destinationPath,
      downloadedOn: new Date().toISOString(),
    }),

    install: (id: string) => ({
      id,
      isInstalled: true,
      installedOn: new Date().toISOString(),
    }),

    uninstall: (id: string) => ({
      id,
      isInstalled: false,
      uninstalledOn: new Date().toISOString(),
    }),

    clone: (id: string, newName: string) => ({
      id: `cloned-${id}`,
      name: newName,
      clonedFrom: id,
      clonedOn: new Date().toISOString(),
    }),

    getVersions: (id: string) => [
      { version: '1.0.0', isActive: true, releasedOn: '2024-01-15' },
      { version: '0.9.0', isActive: false, releasedOn: '2024-01-10' },
    ],

    search: (searchTerm: string) => [
      {
        id: 'demo-oapp-1',
        name: 'Demo Game',
        description: 'A fun demo game',
        type: 'Game',
        rating: 4.5,
        downloads: 150,
      },
      {
        id: 'demo-oapp-2',
        name: 'Demo Tool',
        description: 'A useful demo tool',
        type: 'Tool',
        rating: 4.2,
        downloads: 89,
      },
    ],
  },

  // Template Operations
  template: {
    create: (payload: any) => ({
      id: 'demo-template-1',
      name: payload.name || 'Demo Template',
      description: payload.description || 'A demo template',
      category: payload.category || 'Game',
      version: '1.0.0',
      isPublished: false,
      createdOn: new Date().toISOString(),
    }),

    update: (id: string, payload: any) => ({
      id,
      ...payload,
      updatedOn: new Date().toISOString(),
    }),

    publish: (id: string) => ({
      id,
      isPublished: true,
      publishedOn: new Date().toISOString(),
    }),

    unpublish: (id: string) => ({
      id,
      isPublished: false,
      unpublishedOn: new Date().toISOString(),
    }),

    download: (id: string, destinationPath: string) => ({
      id,
      path: destinationPath,
      downloadedOn: new Date().toISOString(),
    }),

    install: (id: string) => ({
      id,
      isInstalled: true,
      installedOn: new Date().toISOString(),
    }),

    uninstall: (id: string) => ({
      id,
      isInstalled: false,
      uninstalledOn: new Date().toISOString(),
    }),

    clone: (id: string, newName: string) => ({
      id: `cloned-${id}`,
      name: newName,
      clonedFrom: id,
      clonedOn: new Date().toISOString(),
    }),

    getVersions: (id: string) => [
      { version: '1.0.0', isActive: true, releasedOn: '2024-01-15' },
      { version: '0.9.0', isActive: false, releasedOn: '2024-01-10' },
    ],

    search: (searchTerm: string) => [
      {
        id: 'demo-template-1',
        name: 'Game Template',
        description: 'Template for creating games',
        category: 'Game',
        rating: 4.3,
        downloads: 67,
      },
      {
        id: 'demo-template-2',
        name: 'Tool Template',
        description: 'Template for creating tools',
        category: 'Tool',
        rating: 4.1,
        downloads: 45,
      },
    ],
  },

  // Runtime Operations
  runtime: {
    create: (payload: any) => ({
      id: 'demo-runtime-1',
      name: payload.name || 'Demo Runtime',
      description: payload.description || 'A demo runtime environment',
      version: '1.0.0',
      isPublished: false,
      createdOn: new Date().toISOString(),
    }),

    update: (id: string, payload: any) => ({
      id,
      ...payload,
      updatedOn: new Date().toISOString(),
    }),

    publish: (id: string) => ({
      id,
      isPublished: true,
      publishedOn: new Date().toISOString(),
    }),

    unpublish: (id: string) => ({
      id,
      isPublished: false,
      unpublishedOn: new Date().toISOString(),
    }),

    download: (id: string, destinationPath: string) => ({
      id,
      path: destinationPath,
      downloadedOn: new Date().toISOString(),
    }),

    install: (id: string) => ({
      id,
      isInstalled: true,
      installedOn: new Date().toISOString(),
    }),

    uninstall: (id: string) => ({
      id,
      isInstalled: false,
      uninstalledOn: new Date().toISOString(),
    }),

    clone: (id: string, newName: string) => ({
      id: `cloned-${id}`,
      name: newName,
      clonedFrom: id,
      clonedOn: new Date().toISOString(),
    }),

    getVersions: (id: string) => [
      { version: '1.0.0', isActive: true, releasedOn: '2024-01-15' },
      { version: '0.9.0', isActive: false, releasedOn: '2024-01-10' },
    ],

    search: (searchTerm: string) => [
      {
        id: 'demo-runtime-1',
        name: 'Unity Runtime',
        description: 'Runtime for Unity-based applications',
        version: '1.0.0',
        rating: 4.6,
        downloads: 234,
      },
      {
        id: 'demo-runtime-2',
        name: 'Web Runtime',
        description: 'Runtime for web-based applications',
        version: '1.0.0',
        rating: 4.4,
        downloads: 189,
      },
    ],
  },

  // Library Operations
  library: {
    create: (payload: any) => ({
      id: 'demo-library-1',
      name: payload.name || 'Demo Library',
      description: payload.description || 'A demo library',
      version: '1.0.0',
      isPublished: false,
      createdOn: new Date().toISOString(),
    }),

    update: (id: string, payload: any) => ({
      id,
      ...payload,
      updatedOn: new Date().toISOString(),
    }),

    publish: (id: string) => ({
      id,
      isPublished: true,
      publishedOn: new Date().toISOString(),
    }),

    unpublish: (id: string) => ({
      id,
      isPublished: false,
      unpublishedOn: new Date().toISOString(),
    }),

    download: (id: string, destinationPath: string) => ({
      id,
      path: destinationPath,
      downloadedOn: new Date().toISOString(),
    }),

    install: (id: string) => ({
      id,
      isInstalled: true,
      installedOn: new Date().toISOString(),
    }),

    uninstall: (id: string) => ({
      id,
      isInstalled: false,
      uninstalledOn: new Date().toISOString(),
    }),

    clone: (id: string, newName: string) => ({
      id: `cloned-${id}`,
      name: newName,
      clonedFrom: id,
      clonedOn: new Date().toISOString(),
    }),

    getVersions: (id: string) => [
      { version: '1.0.0', isActive: true, releasedOn: '2024-01-15' },
      { version: '0.9.0', isActive: false, releasedOn: '2024-01-10' },
    ],

    search: (searchTerm: string) => [
      {
        id: 'demo-library-1',
        name: 'Math Library',
        description: 'Mathematical functions library',
        version: '1.0.0',
        rating: 4.2,
        downloads: 156,
      },
      {
        id: 'demo-library-2',
        name: 'Graphics Library',
        description: 'Graphics rendering library',
        version: '1.0.0',
        rating: 4.5,
        downloads: 203,
      },
    ],
  },
};
