import {
  ChangeDetectionStrategy, Component, ElementRef, EventEmitter, HostListener,
  Input, OnInit, Output, ViewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ProjectTask } from '../../../models/task.model';

export interface QuickCreateSubmitEvent {
  name: string;
  phaseId: string;
}

export interface QuickCreateFullFormEvent {
  name: string;
  phaseId: string | null;
}

@Component({
  standalone: true,
  selector: 'app-task-quick-create',
  imports: [
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatSelectModule,
    MatTooltipModule,
  ],
  templateUrl: './task-quick-create.html',
  styleUrl: './task-quick-create.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskQuickCreateComponent implements OnInit {
  @ViewChild('nameInput') nameInputRef!: ElementRef<HTMLInputElement>;

  @Input({ required: true }) phases: ProjectTask[] = [];
  @Input() initialStatus: ProjectTask['status'] = 'NotStarted';

  @Output() submitted = new EventEmitter<QuickCreateSubmitEvent>();
  @Output() cancelled = new EventEmitter<void>();
  @Output() openFullForm = new EventEmitter<QuickCreateFullFormEvent>();

  taskName = '';
  selectedPhaseId: string | null = null;
  phaseError = false;

  get useDropdown(): boolean {
    return this.phases.length > 6;
  }

  get isValid(): boolean {
    return this.taskName.trim().length > 0 && this.selectedPhaseId !== null;
  }

  ngOnInit(): void {
    // Auto-select first active phase
    const active = this.phases.find(
      p => !['Completed', 'Cancelled'].includes(p.status)
    );
    this.selectedPhaseId = active?.id ?? (this.phases[0]?.id ?? null);
  }

  @HostListener('keydown.enter', ['$event'])
  onEnter(event: Event): void {
    event.preventDefault();
    this.submit();
  }

  @HostListener('keydown.escape')
  onEscape(): void {
    this.cancelled.emit();
  }

  selectPhase(phaseId: string): void {
    this.selectedPhaseId = phaseId;
    this.phaseError = false;
  }

  submit(): void {
    if (!this.taskName.trim()) return;
    if (!this.selectedPhaseId) {
      this.phaseError = true;
      return;
    }
    this.submitted.emit({ name: this.taskName.trim(), phaseId: this.selectedPhaseId });
  }

  cancel(): void {
    this.cancelled.emit();
  }

  openFull(): void {
    this.openFullForm.emit({ name: this.taskName.trim(), phaseId: this.selectedPhaseId });
  }
}
