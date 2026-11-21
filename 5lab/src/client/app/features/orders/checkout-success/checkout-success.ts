import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-checkout-success-page',
  imports: [RouterLink],
  templateUrl: './checkout-success.html',
  styleUrl: './checkout-success.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CheckoutSuccessPage {
  readonly number = input.required<string>();
}
