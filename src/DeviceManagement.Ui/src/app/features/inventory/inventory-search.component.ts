import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-inventory-search',
  templateUrl: './inventory-search.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [':host { display: block; }']
})
export class InventorySearchComponent {
  @Input() query = '';
  @Input() isSearching = false;
  @Input() errorMessage: string | null = null;

  @Output() readonly queryChanged = new EventEmitter<string>();
}
