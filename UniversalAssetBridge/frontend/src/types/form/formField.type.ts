export type SelectItems = {
  name: string;
  value: string;
};

export type FormField = {
  name: string;
  placeholder: string;
  disabled?: boolean;
  type:
    | "text"
    | "number"
    | "password"
    | "file"
    | "date"
    | "email"
    | "select"
    | "map"
    | "checkbox"
    | "textarea"
    | "multiselect";
  selectItems?: SelectItems[];
  description?: string;
  label?: string;
};

export type FormFieldGroup = {
  title: string;
  fields: FormField[];
};
