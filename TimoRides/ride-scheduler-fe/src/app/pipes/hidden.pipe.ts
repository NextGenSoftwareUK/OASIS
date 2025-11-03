import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'hidden',
})
export class HiddenPipe implements PipeTransform {
  /**
   *
   * @param value string to be transformed
   * @param args number of chars from the right to mask
   * @returns
   */
  transform(value: string | undefined, ...args: unknown[]): string {
    if (!value) return '';
    const isHidden = (args[0] || 0) as number;
    const maskChar = '*';

    value = value
      .split('')
      .map((char, index) => {
        return index < isHidden ? char : maskChar;
      })
      .join('');

    return value;
  }
}
