import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ClimbListComponent } from './climb-list';

describe('ClimbListComponent', () => {
  let component: ClimbListComponent;
  let fixture: ComponentFixture<ClimbListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClimbListComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ClimbListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});