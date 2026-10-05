import { DropdownConfiguration } from '../models/dropdown';

export const idList: DropdownConfiguration = {
  label: 'Select ID type',
  options: [
    {
      name: 'Passport',
      value: 'Passport',
    },
    {
      name: 'Drivers License',
      value: 'Drivers License',
    },
    {
      name: 'ID Card',
      value: 'ID Card',
    },
  ],
};

export const titles: DropdownConfiguration = {
  label: 'Select a title',
  options: [
    {
      name: 'Mr.',
      value: 'Mr.',
    },
    {
      name: 'Mrs.',
      value: 'Mrs.',
    },
    {
      name: 'Ms.',
      value: 'Ms.',
    },
    {
      name: 'Dr.',
      value: 'Dr.',
    },
    {
      name: 'Prof.',
      value: 'Prof.',
    },
  ],
};

export const banks: DropdownConfiguration = {
  label: 'Select bank',
  options: [
    {
      name: 'Bank 1',
      value: 'Bank 1',
    },
    {
      name: 'Bank 2',
      value: 'Bank 2',
    },
    {
      name: 'Bank 3',
      value: 'Bank 3',
    },
    {
      name: 'Bank 4',
      value: 'Bank 4',
    },
    {
      name: 'Bank 5',
      value: 'Bank 5',
    },
  ],
};

export const accountTypes: DropdownConfiguration = {
  label: 'Select bank',
  options: [
    {
      name: 'Savings',
      value: 'Savings',
    },
    {
      name: 'Current',
      value: 'Current',
    },
  ],
};
