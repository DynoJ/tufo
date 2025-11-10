import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SubArea } from '../../services/areas.service';

@Component({
  selector: 'app-area-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './area-card.html',
  styleUrls: ['./area-card.scss']
})
export class AreaCardComponent {
  @Input() area!: SubArea;
  @Output() areaClick = new EventEmitter<number>();

  onCardClick(): void {
    this.areaClick.emit(this.area.id);
  }
}