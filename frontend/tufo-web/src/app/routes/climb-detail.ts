import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiService, Climb, Media } from '../services/api';

@Component({
  selector: 'app-climb-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './climb-detail.html',
  styleUrls: ['./climb-detail.scss']
})
export class ClimbDetailComponent implements OnInit {
  climb?: Climb;
  noteText = '';
  file?: File;
  caption = '';
  posting = false;
  uploading = false;

  constructor(private route: ActivatedRoute, private api: ApiService) {}

  ngOnInit(): void { this.load(); }

  private load() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.api.getClimbById(id).subscribe(c => this.climb = c);
  }

  addNote() {
    if (!this.climb || !this.noteText.trim()) return;
    console.log('Posting note to climb ID:', this.climb.id);
    this.posting = true;
    this.api.addNote(this.climb.id, this.noteText.trim()).subscribe({
      next: n => { this.climb!.notes = [n, ...this.climb!.notes]; this.noteText=''; this.posting=false; },
      error: _ => this.posting=false
  });
  }

  onFileChange(e: Event) {
    const input = e.target as HTMLInputElement;
    this.file = input.files && input.files[0] ? input.files[0] : undefined;
  }

  isVideo(m: Media) { return m.type === 1; } // 0=Photo, 1=Video

  upload() {
    if (!this.climb || !this.file) return;
    this.uploading = true;
    this.api.uploadMedia(this.climb.id, this.file, this.caption).subscribe({
      next: m => {
        this.climb!.media = [m, ...this.climb!.media];
        this.file = undefined; this.caption = '';
        (document.getElementById('fileInput') as HTMLInputElement).value = '';
        this.uploading = false;
      },
      error: _ => this.uploading = false
    });
  }
}