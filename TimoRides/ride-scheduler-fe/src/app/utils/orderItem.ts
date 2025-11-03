import { KeyValue } from '@angular/common';

export const orderItem = (
  a: KeyValue<string, string>,
  b: KeyValue<string, string>
) => {
  return a > b ? -1 : 1;
};
