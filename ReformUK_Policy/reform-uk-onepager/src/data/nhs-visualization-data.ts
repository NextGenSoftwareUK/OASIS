// NHS Waiting Lists - 3D Network Visualization Data
// Adapts OASIS Oracle 3D visualization for NHS resource tracking

export type NHSNode3D = {
  id: string;
  name: string;
  type: 'hospital' | 'trust' | 'gp' | 'specialist';
  position: [number, number, number];
  color: string;
  patients: number; // Total patients
  waitingList: number; // Current waiting list
  beds: number; // Total beds
  bedsAvailable: number; // Available now
  staff: number; // Total staff
  staffAvailable: number; // On duty now
  equipment: { mri: number; ct: number; xray: number; ventilators: number };
  capacity: number; // Utilization %
  avgWaitTime: number; // Days
  region: string;
};

export type PatientFlow3D = {
  from: string; // Hospital ID
  to: string; // Hospital ID
  amount: number; // Number of patients
  type: 'transfer' | 'referral' | 'ambulance';
  isActive: boolean;
};

// Mock NHS hospitals and trusts (London area for demo)
export const nhsNodes3D: NHSNode3D[] = [
  // Tier 1: Major London Hospitals (top layer)
  {
    id: 'st-thomas',
    name: "St Thomas' Hospital",
    type: 'hospital',
    position: [0, 12, 0],
    color: '#ef4444', // Red (overcapacity)
    patients: 15000,
    waitingList: 2500,
    beds: 840,
    bedsAvailable: 15, // 98% full
    staff: 4200,
    staffAvailable: 850,
    equipment: { mri: 5, ct: 8, xray: 20, ventilators: 50 },
    capacity: 98,
    avgWaitTime: 245, // days
    region: 'London'
  },
  {
    id: 'royal-london',
    name: 'Royal London Hospital',
    type: 'hospital',
    position: [10, 10, -5],
    color: '#facc15', // Yellow (high capacity)
    patients: 12000,
    waitingList: 1800,
    beds: 675,
    bedsAvailable: 45,
    staff: 3500,
    staffAvailable: 720,
    equipment: { mri: 4, ct: 6, xray: 18, ventilators: 40 },
    capacity: 93,
    avgWaitTime: 180,
    region: 'London'
  },
  {
    id: 'ucl-hospital',
    name: 'University College Hospital',
    type: 'hospital',
    position: [-10, 10, -5],
    color: '#facc15',
    patients: 11500,
    waitingList: 1600,
    beds: 665,
    bedsAvailable: 50,
    staff: 3400,
    staffAvailable: 700,
    equipment: { mri: 5, ct: 7, xray: 16, ventilators: 45 },
    capacity: 92,
    avgWaitTime: 165,
    region: 'London'
  },
  
  // Tier 2: District General Hospitals (mid layer)
  {
    id: 'chelsea-westminster',
    name: 'Chelsea & Westminster Hospital',
    type: 'hospital',
    position: [8, 5, 8],
    color: '#22d3ee', // Cyan (good capacity)
    patients: 8000,
    waitingList: 800,
    beds: 430,
    bedsAvailable: 85,
    staff: 2100,
    staffAvailable: 450,
    equipment: { mri: 3, ct: 4, xray: 12, ventilators: 25 },
    capacity: 80,
    avgWaitTime: 90,
    region: 'London'
  },
  {
    id: 'kings-college',
    name: "King's College Hospital",
    type: 'hospital',
    position: [-8, 5, 8],
    color: '#ef4444',
    patients: 14000,
    waitingList: 2200,
    beds: 850,
    bedsAvailable: 12,
    staff: 4500,
    staffAvailable: 900,
    equipment: { mri: 6, ct: 9, xray: 22, ventilators: 55 },
    capacity: 99,
    avgWaitTime: 280,
    region: 'London'
  },
  {
    id: 'guys-hospital',
    name: "Guy's Hospital",
    type: 'hospital',
    position: [8, 5, -8],
    color: '#facc15',
    patients: 10000,
    waitingList: 1400,
    beds: 550,
    bedsAvailable: 35,
    staff: 2800,
    staffAvailable: 580,
    equipment: { mri: 4, ct: 5, xray: 14, ventilators: 35 },
    capacity: 94,
    avgWaitTime: 150,
    region: 'London'
  },
  {
    id: 'whipps-cross',
    name: 'Whipps Cross Hospital',
    type: 'hospital',
    position: [-8, 5, -8],
    color: '#22d3ee',
    patients: 7000,
    waitingList: 650,
    beds: 380,
    bedsAvailable: 75,
    staff: 1800,
    staffAvailable: 400,
    equipment: { mri: 2, ct: 3, xray: 10, ventilators: 20 },
    capacity: 80,
    avgWaitTime: 75,
    region: 'London'
  },
  
  // Tier 3: Specialist Centers (lower layer)
  {
    id: 'moorfields',
    name: 'Moorfields Eye Hospital',
    type: 'specialist',
    position: [5, 0, 5],
    color: '#22d3ee',
    patients: 3000,
    waitingList: 400,
    beds: 120,
    bedsAvailable: 25,
    staff: 900,
    staffAvailable: 200,
    equipment: { mri: 1, ct: 2, xray: 6, ventilators: 5 },
    capacity: 79,
    avgWaitTime: 60,
    region: 'London'
  },
  {
    id: 'great-ormond',
    name: 'Great Ormond Street (Children)',
    type: 'specialist',
    position: [-5, 0, 5],
    color: '#22d3ee',
    patients: 5000,
    waitingList: 550,
    beds: 395,
    bedsAvailable: 45,
    staff: 2400,
    staffAvailable: 520,
    equipment: { mri: 3, ct: 4, xray: 8, ventilators: 30 },
    capacity: 89,
    avgWaitTime: 95,
    region: 'London'
  },
  {
    id: 'royal-brompton',
    name: 'Royal Brompton (Heart & Lung)',
    type: 'specialist',
    position: [5, 0, -5],
    color: '#facc15',
    patients: 6000,
    waitingList: 900,
    beds: 245,
    bedsAvailable: 18,
    staff: 1500,
    staffAvailable: 320,
    equipment: { mri: 2, ct: 3, xray: 7, ventilators: 40 },
    capacity: 93,
    avgWaitTime: 140,
    region: 'London'
  },
  {
    id: 'royal-marsden',
    name: 'Royal Marsden (Cancer)',
    type: 'specialist',
    position: [-5, 0, -5],
    color: '#ef4444',
    patients: 8000,
    waitingList: 1200,
    beds: 280,
    bedsAvailable: 8,
    staff: 1900,
    staffAvailable: 390,
    equipment: { mri: 4, ct: 6, xray: 12, ventilators: 25 },
    capacity: 97,
    avgWaitTime: 210,
    region: 'London'
  },
];

// Patient transfer flows between hospitals
export const patientFlows3D: PatientFlow3D[] = [
  { from: 'st-thomas', to: 'chelsea-westminster', amount: 45, type: 'transfer', isActive: true },
  { from: 'royal-london', to: 'whipps-cross', amount: 32, type: 'transfer', isActive: true },
  { from: 'kings-college', to: 'guys-hospital', amount: 28, type: 'transfer', isActive: true },
  { from: 'ucl-hospital', to: 'moorfields', amount: 18, type: 'referral', isActive: true },
  { from: 'st-thomas', to: 'royal-brompton', amount: 22, type: 'referral', isActive: true },
  { from: 'royal-london', to: 'great-ormond', amount: 15, type: 'referral', isActive: true },
  { from: 'kings-college', to: 'royal-marsden', amount: 35, type: 'referral', isActive: true },
  { from: 'ucl-hospital', to: 'chelsea-westminster', amount: 12, type: 'ambulance', isActive: true },
];

// Calculate total NHS stats
export function getNHSStats() {
  const totalPatients = nhsNodes3D.reduce((sum, node) => sum + node.patients, 0);
  const totalWaitingList = nhsNodes3D.reduce((sum, node) => sum + node.waitingList, 0);
  const totalBeds = nhsNodes3D.reduce((sum, node) => sum + node.beds, 0);
  const totalBedsAvailable = nhsNodes3D.reduce((sum, node) => sum + node.bedsAvailable, 0);
  const avgCapacity = nhsNodes3D.reduce((sum, node) => sum + node.capacity, 0) / nhsNodes3D.length;
  const totalTransfers = patientFlows3D.filter(f => f.isActive).reduce((sum, f) => sum + f.amount, 0);
  
  return {
    totalPatients,
    totalWaitingList,
    totalBeds,
    totalBedsAvailable,
    bedsOccupied: totalBeds - totalBedsAvailable,
    avgCapacity: Math.round(avgCapacity),
    totalTransfers,
    hospitalsMonitored: nhsNodes3D.length,
    activeFlows: patientFlows3D.filter(f => f.isActive).length,
  };
}

// Get hospital node by ID
export function getHospitalNode(id: string): NHSNode3D | undefined {
  return nhsNodes3D.find(node => node.id === id);
}

// Calculate node size based on waiting list
export function calculateNHSNodeSize(waitingList: number): number {
  return Math.max(0.5, Math.log10(waitingList + 1) * 0.5 + 0.5);
}

// Get health color based on capacity
export function getHealthColor(capacity: number): string {
  if (capacity >= 95) return '#ef4444'; // Red (critical)
  if (capacity >= 85) return '#facc15'; // Yellow (high)
  return '#22d3ee'; // Cyan (good)
}

// Format waiting time
export function formatWaitTime(days: number): string {
  if (days < 30) return `${days} days`;
  const weeks = Math.floor(days / 7);
  if (weeks < 8) return `${weeks} weeks`;
  const months = Math.floor(days / 30);
  return `${months} months`;
}



