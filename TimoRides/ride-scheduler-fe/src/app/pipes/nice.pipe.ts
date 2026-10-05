import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'nice',
})
export class NicePipe implements PipeTransform {
  transform(text: string, ...args: string[]): string {
    // set default length if not provided
    const defaultLength = +args[0] || 10;

    // Format string based on current string length
    if (text) {
      if (text.length > defaultLength) {
        return `${text.substring(0, defaultLength).trim()}...`;
      }
    }
    return text;
  }
}
