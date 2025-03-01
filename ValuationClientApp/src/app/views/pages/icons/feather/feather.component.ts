import { Component } from '@angular/core';
import { FeatherIconDirective } from '../../../../core/feather-icon/feather-icon.directive';
import { RouterLink } from '@angular/router';

@Component({
    selector: 'app-feather',
    imports: [
        RouterLink,
        FeatherIconDirective
    ],
    templateUrl: './feather.component.html'
})
export class FeatherComponent {

}
