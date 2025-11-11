import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClimbDetail } from './climb-detail';

describe('ClimbDetail', () => {
  let component: ClimbDetail;
  let fixture: ComponentFixture<ClimbDetail>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClimbDetail]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ClimbDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
