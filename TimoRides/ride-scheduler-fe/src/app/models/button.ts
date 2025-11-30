export type IButtonConfig = {
  label: string;
  block?: boolean;
  size: 'sm' | 'md' | 'lg';
  theme:
    | 'primary'
    | 'secondary'
    | 'danger'
    | 'primary-outline'
    | 'secondary-outline'
    | 'danger-outline';
  disabled?: boolean;
  isActive?: boolean;
};
