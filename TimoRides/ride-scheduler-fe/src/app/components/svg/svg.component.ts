import { Component, Input, OnChanges } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer } from '@angular/platform-browser';

@Component({
  selector: 'app-svg',
  template: `<span [innerHTML]="svgIcon"></span>`,
  styleUrls: ['./svg.component.css'],
})
export class SvgComponent implements OnChanges {
  @Input()
  public fileName?: string;

  public svgIcon: any;

  constructor(
    private httpClient: HttpClient,
    private sanitizer: DomSanitizer
  ) {}

  public ngOnChanges(): void {
    if (!this.fileName) {
      this.svgIcon = '';
      return;
    }
    this.httpClient
      .get(`assets/images/${this.fileName}.svg`, { responseType: 'text' })
      .subscribe((value) => {
        this.svgIcon = this.sanitizer.bypassSecurityTrustHtml(value);
      });
  }
}
