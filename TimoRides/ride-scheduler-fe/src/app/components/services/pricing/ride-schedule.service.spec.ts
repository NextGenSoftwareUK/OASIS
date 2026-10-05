import { TestBed } from '@angular/core/testing';

import { RideScheduleService } from './ride-schedule.service';

describe('RideScheduleService', () => {
  let service: RideScheduleService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(RideScheduleService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
