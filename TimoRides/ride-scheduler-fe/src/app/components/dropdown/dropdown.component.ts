/* eslint-disable @typescript-eslint/no-empty-function */
/* eslint-disable @typescript-eslint/no-explicit-any */
/* eslint-disable @typescript-eslint/no-unused-vars */
import {
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  Output,
  forwardRef,
} from '@angular/core';
import {
  DropdownConfiguration,
  OptionType,
  ReturnedValue,
  SelectedOption,
} from './../../models/dropdown';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-dropdown',
  templateUrl: './dropdown.component.html',
  styleUrls: ['./dropdown.component.css'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DropdownComponent),
      multi: true,
    },
  ],
})
export class DropdownComponent implements ControlValueAccessor {
  @Input() dropdownConfiguration: DropdownConfiguration | undefined;
  /** @Input Sets the default state of the dropdown */
  @Input() opened = false;
  /** @OutputEvent Triggers event when as the dropdown opens and closes */
  @Output() handleDropdownToggle: EventEmitter<boolean> = new EventEmitter();
  /** @OutputEvent Triggers event when a dropdown item is selected */
  @Output() handleItemSelect: EventEmitter<OptionType> = new EventEmitter();
  @Input() selectedOptionDisplay: SelectedOption = '';
  @Input() disabled = false;
  selectedOption: ReturnedValue | undefined;
  isOpen = this.opened;

  onChange: any = () => {};
  onTouched: any = () => {};

  constructor(private elementRef: ElementRef) {}

  ngOnChanges() {
    if (this.dropdownConfiguration) {
      this.disabled = this.dropdownConfiguration.disabled || false;
    }
  }

  toggleDropdown(): void {
    if (this.disabled) return;
    this.isOpen = !this.isOpen;
    this.handleDropdownToggle?.emit(this.isOpen);
  }

  selectOption(event: Event, option: OptionType): void {
    if (this.disabled) return;

    const selectedItem: ReturnedValue = {
      event,
      option,
    };

    this.selectedOptionDisplay = option.name;
    this.selectedOption = selectedItem;
    this.closeDropdown();

    this.onChange(this.selectedOption.option.value);

    this.onTouched();

    this.handleItemSelect?.emit(option);
  }

  closeDropdown(): void {
    this.isOpen = false;
  }

  @HostListener('document:click', ['$event'])
  clickOutside(event: Event): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.closeDropdown();
    }
  }

  // Implement ControlValueAccessor methods
  writeValue(value: any): void {
    // Update the selected value based on the value passed from the form control
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}
