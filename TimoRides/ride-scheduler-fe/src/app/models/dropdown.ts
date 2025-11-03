export type DropdownConfiguration = {
  label: string;
  options: OptionType[] | null;
  selectedOption?: SelectedOption;
  search?: boolean;
  disabled?: boolean;
};

export type OptionType = { name: string; value?: string | number | boolean };

export type SelectedOption = string | number | boolean | null;

export type ReturnedValue = {
  event: Event;
  option: OptionType;
};
