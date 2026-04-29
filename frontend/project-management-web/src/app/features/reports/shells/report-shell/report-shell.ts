import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  standalone: true,
  selector: 'app-report-shell',
  imports: [RouterOutlet],
  template: `<router-outlet />`,
  styles: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportShellComponent {}
