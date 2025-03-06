
import { AfterViewInit, OnInit, Component, TemplateRef } from '@angular/core';
import { NgbDropdownModule, NgbNavModule, NgbTooltip, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { NgScrollbarModule } from 'ngx-scrollbar';


@Component({
  selector: 'app-template-list',
  imports: [
    NgbNavModule,
    NgbDropdownModule,
    NgScrollbarModule,
    NgbTooltip
],
  templateUrl: './template-list.component.html',
  styleUrl: './template-list.component.scss'
})
export class TemplateListComponent implements AfterViewInit {
  defaultNavActiveId = 1;
  basicModalCode: any;
    scrollableModalCode: any;
    verticalCenteredModalCode: any;
    optionalSizesModalCode: any;
  
    basicModalCloseResult: string = '';
  
    constructor(private modalService: NgbModal) { }
  
    ngOnInit(): void {
     
    }

  ngAfterViewInit(): void {

    // Show the chat-content when a chat-item is clicked on tablet and mobile devices
    // document.querySelectorAll('.chat-list .chat-item').forEach(item => {
    //   item.addEventListener('click', event => {
    //     document.querySelector('.chat-content')!.classList.toggle('show');
    //   })
    // });

  }

    openLgModal(content: TemplateRef<any>) {
      this.modalService.open(content, {size: 'lg'}).result.then((result) => {
        console.log("Modal closed" + result);
      }).catch((res) => {});
    }

  // Back to the chat-list on tablet and mobile devices
  // backToChatList() {
  //   document.querySelector('.chat-content')!.classList.toggle('show');
  // }

  save() {
    console.log('passs');
  }
}
