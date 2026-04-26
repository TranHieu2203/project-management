import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AsyncPipe } from '@angular/common';
import { Store } from '@ngrx/store';
import { ProjectsActions } from '../../store/projects.actions';
import { selectProjectsCreating, selectProjectsUpdating } from '../../store/projects.selectors';
import { Project } from '../../models/project.model';

export interface ProjectFormData {
  mode: 'create' | 'edit';
  project?: Project;
}

@Component({
  selector: 'app-project-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    AsyncPipe,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './project-form.html',
  styleUrl: './project-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly store = inject(Store);
  private readonly dialogRef = inject(MatDialogRef<ProjectFormComponent>);
  readonly data = inject<ProjectFormData>(MAT_DIALOG_DATA);

  protected readonly creating$ = this.store.select(selectProjectsCreating);
  protected readonly updating$ = this.store.select(selectProjectsUpdating);

  protected readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.maxLength(20), Validators.pattern(/^[A-Z0-9\-]+$/)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(1000)]],
  });

  get isEdit(): boolean {
    return this.data.mode === 'edit';
  }

  ngOnInit(): void {
    if (this.isEdit && this.data.project) {
      this.form.patchValue({
        code: this.data.project.code,
        name: this.data.project.name,
        description: this.data.project.description ?? '',
      });
      this.form.get('code')!.disable();
    }
  }

  submit(): void {
    if (this.form.invalid) return;

    const { code, name, description } = this.form.getRawValue();

    if (this.isEdit && this.data.project) {
      this.store.dispatch(
        ProjectsActions.updateProject({
          projectId: this.data.project.id,
          name,
          description: description || undefined,
          version: this.data.project.version,
        })
      );
    } else {
      this.store.dispatch(
        ProjectsActions.createProject({
          code,
          name,
          description: description || undefined,
        })
      );
    }

    this.dialogRef.close();
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
