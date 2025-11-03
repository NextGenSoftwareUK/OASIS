// Government Waste Dashboard - 3D Spending Visualization Data
// Shows real-time government spending across departments

export type DepartmentNode3D = {
  id: string;
  name: string;
  shortName: string;
  position: [number, number, number];
  color: string;
  annualBudget: number; // £ billions
  spentToday: number; // £ millions
  frontlinePercent: number; // % spent on frontline vs. back-office
  wasteScore: number; // 0-100 (higher = more waste)
  efficiency: number; // 0-100 (higher = better)
  contracts: number; // Active contracts
  transparency: number; // 0-100 (blockchain integration %)
};

export type SpendingFlow3D = {
  from: string; // Department ID
  to: string; // Recipient (contractor, benefit, service)
  amount: number; // £ millions
  type: 'procurement' | 'payroll' | 'benefits' | 'services';
  isWasteful: boolean;
  isActive: boolean;
};

// UK Government Departments (3D positioned)
export const departmentNodes3D: DepartmentNode3D[] = [
  // Tier 1: Largest Departments (top layer)
  {
    id: 'nhs',
    name: 'National Health Service',
    shortName: 'NHS',
    position: [0, 12, 0],
    color: '#ef4444', // Red (high waste)
    annualBudget: 180,
    spentToday: 493, // £493M/day
    frontlinePercent: 60, // 40% back-office
    wasteScore: 75,
    efficiency: 65,
    contracts: 12500,
    transparency: 15, // Low blockchain integration
  },
  {
    id: 'defence',
    name: 'Ministry of Defence',
    shortName: 'MOD',
    position: [12, 10, 0],
    color: '#facc15', // Yellow (medium waste)
    annualBudget: 54,
    spentToday: 148,
    frontlinePercent: 70,
    wasteScore: 60,
    efficiency: 70,
    contracts: 8500,
    transparency: 25,
  },
  {
    id: 'education',
    name: 'Department for Education',
    shortName: 'DfE',
    position: [-12, 10, 0],
    color: '#22d3ee', // Cyan (good efficiency)
    annualBudget: 116,
    spentToday: 318,
    frontlinePercent: 82, // Good frontline spending
    wasteScore: 35,
    efficiency: 85,
    contracts: 4200,
    transparency: 45,
  },
  {
    id: 'dwp',
    name: 'Work & Pensions',
    shortName: 'DWP',
    position: [10, 10, 10],
    color: '#ef4444', // Red (benefit fraud)
    annualBudget: 234,
    spentToday: 641,
    frontlinePercent: 88, // Benefits are "frontline"
    wasteScore: 80, // High fraud
    efficiency: 55,
    contracts: 1500,
    transparency: 10,
  },
  
  // Tier 2: Medium Departments (mid layer)
  {
    id: 'home-office',
    name: 'Home Office',
    shortName: 'Home Office',
    position: [6, 5, -6],
    color: '#facc15',
    annualBudget: 18,
    spentToday: 49,
    frontlinePercent: 65,
    wasteScore: 55,
    efficiency: 72,
    contracts: 3200,
    transparency: 20,
  },
  {
    id: 'transport',
    name: 'Department for Transport',
    shortName: 'DfT',
    position: [-6, 5, -6],
    color: '#ef4444', // Red (HS2 waste)
    annualBudget: 38,
    spentToday: 104,
    frontlinePercent: 45, // Lots of consultants
    wasteScore: 90, // HS2 scandal
    efficiency: 45,
    contracts: 5600,
    transparency: 12,
  },
  {
    id: 'hmrc',
    name: 'HM Revenue & Customs',
    shortName: 'HMRC',
    position: [6, 5, 6],
    color: '#22d3ee',
    annualBudget: 6.8,
    spentToday: 19,
    frontlinePercent: 75,
    wasteScore: 40,
    efficiency: 78,
    contracts: 800,
    transparency: 55, // Better IT systems
  },
  {
    id: 'justice',
    name: 'Ministry of Justice',
    shortName: 'MOJ',
    position: [-6, 5, 6],
    color: '#facc15',
    annualBudget: 9.2,
    spentToday: 25,
    frontlinePercent: 68,
    wasteScore: 50,
    efficiency: 73,
    contracts: 2100,
    transparency: 30,
  },
  
  // Tier 3: Smaller Departments (lower layer)
  {
    id: 'treasury',
    name: 'HM Treasury',
    shortName: 'Treasury',
    position: [0, 0, 0], // Center (controls all spending)
    color: '#22d3ee',
    annualBudget: 12,
    spentToday: 33,
    frontlinePercent: 90,
    wasteScore: 25, // Low waste (controls purse strings)
    efficiency: 92,
    contracts: 200,
    transparency: 70, // Should be highest
  },
  {
    id: 'foreign-office',
    name: 'Foreign, Commonwealth & Development Office',
    shortName: 'FCDO',
    position: [4, -2, 4],
    color: '#facc15',
    annualBudget: 13.2,
    spentToday: 36,
    frontlinePercent: 55, // Lots of admin
    wasteScore: 60,
    efficiency: 65,
    contracts: 1800,
    transparency: 18,
  },
  {
    id: 'culture',
    name: 'Digital, Culture, Media & Sport',
    shortName: 'DCMS',
    position: [-4, -2, 4],
    color: '#22d3ee',
    annualBudget: 1.6,
    spentToday: 4.4,
    frontlinePercent: 80,
    wasteScore: 30,
    efficiency: 82,
    contracts: 650,
    transparency: 35,
  },
];

// Real-time spending flows (updated every second in real system)
export const spendingFlows3D: SpendingFlow3D[] = [
  // Wasteful spending (red/yellow departments)
  { from: 'nhs', to: 'Management Consultants', amount: 125, type: 'procurement', isWasteful: true, isActive: true },
  { from: 'transport', to: 'HS2 Contractors', amount: 85, type: 'procurement', isWasteful: true, isActive: true },
  { from: 'dwp', to: 'Benefit Fraud', amount: 65, type: 'benefits', isWasteful: true, isActive: true },
  { from: 'defence', to: 'Overpriced Equipment', amount: 55, type: 'procurement', isWasteful: true, isActive: true },
  { from: 'nhs', to: 'Agency Staff (Premium)', amount: 45, type: 'payroll', isWasteful: true, isActive: true },
  
  // Efficient spending (cyan departments)
  { from: 'education', to: 'Teacher Salaries', amount: 180, type: 'payroll', isWasteful: false, isActive: true },
  { from: 'hmrc', to: 'Tax Collection System', amount: 12, type: 'procurement', isWasteful: false, isActive: true },
  { from: 'treasury', to: 'Debt Interest', amount: 95, type: 'services', isWasteful: false, isActive: true },
  
  // Mixed
  { from: 'home-office', to: 'Border Control Tech', amount: 15, type: 'procurement', isWasteful: false, isActive: true },
  { from: 'justice', to: 'Prison Services', amount: 18, type: 'services', isWasteful: false, isActive: true },
];

// Calculate total government stats
export function getGovernmentStats() {
  const totalBudget = departmentNodes3D.reduce((sum, dept) => sum + dept.annualBudget, 0);
  const totalSpentToday = departmentNodes3D.reduce((sum, dept) => sum + dept.spentToday, 0);
  const totalWastefulSpending = spendingFlows3D
    .filter(flow => flow.isWasteful && flow.isActive)
    .reduce((sum, flow) => sum + flow.amount, 0);
  const totalEfficientSpending = spendingFlows3D
    .filter(flow => !flow.isWasteful && flow.isActive)
    .reduce((sum, flow) => sum + flow.amount, 0);
  const avgTransparency = departmentNodes3D.reduce((sum, dept) => sum + dept.transparency, 0) / departmentNodes3D.length;
  const avgEfficiency = departmentNodes3D.reduce((sum, dept) => sum + dept.efficiency, 0) / departmentNodes3D.length;
  
  return {
    totalBudget,
    totalSpentToday,
    totalWastefulSpending,
    totalEfficientSpending,
    wastePercent: Math.round((totalWastefulSpending / (totalWastefulSpending + totalEfficientSpending)) * 100),
    avgTransparency: Math.round(avgTransparency),
    avgEfficiency: Math.round(avgEfficiency),
    departmentsMonitored: departmentNodes3D.length,
    activeFlows: spendingFlows3D.filter(f => f.isActive).length,
  };
}

// Get department by ID
export function getDepartment(id: string): DepartmentNode3D | undefined {
  return departmentNodes3D.find(dept => dept.id === id);
}

// Format currency (£ billions or millions)
export function formatGBP(amount: number, unit: 'bn' | 'm' = 'bn'): string {
  if (unit === 'bn') {
    return `£${amount.toFixed(1)}bn`;
  }
  return `£${amount.toFixed(0)}m`;
}

// Calculate potential savings with blockchain transparency
export function calculateBlockchainSavings(department: DepartmentNode3D): number {
  // Higher waste score = more potential savings
  // Formula: Budget * (WasteScore/100) * (1 - Transparency/100) * 0.5
  // Assumes blockchain can cut waste in half for untransparent departments
  return department.annualBudget * (department.wasteScore / 100) * (1 - department.transparency / 100) * 0.5;
}

// Get total potential savings across all departments
export function getTotalBlockchainSavings(): number {
  return departmentNodes3D.reduce((sum, dept) => sum + calculateBlockchainSavings(dept), 0);
}

