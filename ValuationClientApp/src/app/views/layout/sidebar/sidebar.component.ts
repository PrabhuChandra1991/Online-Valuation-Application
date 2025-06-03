import { DOCUMENT, NgClass } from '@angular/common';
import { AfterViewInit, Component, ElementRef, Inject, OnInit, Renderer2, ViewChild } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';

import { NgScrollbar } from 'ngx-scrollbar';
import MetisMenu from 'metismenujs';

import { MENU } from './menu';
import { MenuItem } from './menu.model';

import { FeatherIconDirective } from '../../../core/feather-icon/feather-icon.directive';
import { SharedService } from '../../pages/services/shared.service'


@Component({
    selector: 'app-sidebar',
    imports: [
        RouterLink,
        RouterLinkActive,
        NgScrollbar,
        NgClass,
        FeatherIconDirective

    ],
    templateUrl: './sidebar.component.html',
    styleUrl: './sidebar.component.scss'
})
export class SidebarComponent implements OnInit, AfterViewInit {

  logoURL: string = 'images/skcet-logo-lg.png';

  menuItems: MenuItem[] = [];
  masterMenu : MenuItem[] = [];

  @ViewChild('sidebarToggler') sidebarToggler: ElementRef;
  @ViewChild('sidebarMenu') sidebarMenu: ElementRef;

  expanded: boolean = true;
  @ViewChild('toggleLeftIcon') toggleLeftIcon!: ElementRef;
  // @ViewChild('toggleRightIcon') toggleRightIcon!: ElementRef;

  constructor(@Inject(DOCUMENT) private document: Document, private renderer: Renderer2, router: Router, private sharedService: SharedService) {
    router.events.forEach((event) => {
      if (event instanceof NavigationEnd) {

        /**
         * Activating the current active item dropdown
         */
        this._activateMenuDropdown();

        /**
         * closing the sidebar
         */
        if (window.matchMedia('(max-width: 991px)').matches) {
          this.document.body.classList.remove('sidebar-open');
        }

      }
    });
  }

  ngOnInit(): void {

    this.masterMenu = MENU;

    const loggedData = localStorage.getItem('userData');

    if(loggedData)
    {
      const userData = JSON.parse(loggedData);

      this.menuItems = this.masterMenu.filter(x=>x.role.includes(userData.roleId));

      console.log(this.menuItems);
    }
    /**
     * Sidebar-folded on desktop (min-width:992px and max-width: 1199px)
     */
    const desktopMedium = window.matchMedia('(min-width:992px) and (max-width: 1199px)');
    desktopMedium.addEventListener('change', () => {
      this.iconSidebar;
    });
    this.iconSidebar(desktopMedium);
  }

  ngAfterViewInit() {
      
    console.log("toggleLeftIcon", this.toggleLeftIcon)
    const ri = this.toggleLeftIcon.nativeElement;
    ri.style.display = 'none';

    // activate menu items
    new MetisMenu(this.sidebarMenu.nativeElement);

    this._activateMenuDropdown();
  }

  /**
   * Toggle the sidebar when the hamburger button is clicked
   */
  toggleSidebar(e: Event) {
    this.sidebarToggler.nativeElement.classList.toggle('active');
    if (window.matchMedia('(min-width: 992px)').matches) {
      e.preventDefault();
      this.document.body.classList.toggle('sidebar-folded');
    } else if (window.matchMedia('(max-width: 991px)').matches) {
      e.preventDefault();
      this.document.body.classList.toggle('sidebar-open');
    }

    // const li = this.toggleLeftIcon.nativeElement;
    // const ri = this.toggleRightIcon.nativeElement;
    // // console.log(el.getAttribute("data-feather"))
    // // if (el.getAttribute("data-feather") == "arrow-left-circle") {
    // //   el.setAttribute('data-feather', "arrow-right-circle");
    // // }
    // // else {
    // //   el.setAttribute('data-feather', "arrow-left-circle");
    // // }
    // // console.log(el.getAttribute("data-feather"))
    
    if (this.logoURL == "images/skcet-logo.png") {
      this.logoURL = "images/skcet-logo-lg.png";
      this.expanded = true;
      //el.setAttribute('data-feather', "arrow-left-circle");
      // li.style.display = 'none';
      // ri.style.display = 'none';
    }
    else {
      this.logoURL = "images/skcet-logo.png";
      this.expanded = false;
      //el.setAttribute('data-feather', "arrow-right-circle");
      // li.style.display = 'none';
      // ri.style.display = 'none';
    }

    // console.log("this.expanded", this.expanded)

    this.triggerPDFRefresh();
  }


  /**
   * Open the sidebar on hover when it is in a folded state
   */
  operSidebarFolded() {
    if (this.document.body.classList.contains('sidebar-folded')){
      this.document.body.classList.add("open-sidebar-folded");
    }
  }


  /**
   * Fold sidebar after mouse leave (in folded state)
   */
  closeSidebarFolded() {
    if (this.document.body.classList.contains('sidebar-folded')){
      this.document.body.classList.remove("open-sidebar-folded");
    }
  }

  /**
   * Sidebar folded on desktop screens with a width between 992px and 1199px
   */
  iconSidebar(mq: MediaQueryList) {
    if (mq.matches) {
      this.document.body.classList.add('sidebar-folded');
    } else {
      this.document.body.classList.remove('sidebar-folded');
    }
  }


  /**
   * Returns true or false depending on whether the given menu item has a child
   * @param item menuItem
   */
  hasItems(item: MenuItem) {
    return item.subItems !== undefined ? item.subItems.length > 0 : false;
  }


  /**
   * Reset the menus, then highlight the currently active menu item
   */
  _activateMenuDropdown() {
    this.resetMenuItems();
    this.activateMenuItems();
  }


  /**
   * Resets the menus
   */
  resetMenuItems() {

    const links = document.getElementsByClassName('nav-link-ref');

    for (let i = 0; i < links.length; i++) {
      const menuItemEl = links[i];
      menuItemEl.classList.remove('mm-active');
      const parentEl = menuItemEl.parentElement;

      if (parentEl) {
          parentEl.classList.remove('mm-active');
          const parent2El = parentEl.parentElement;

          if (parent2El) {
            parent2El.classList.remove('mm-show');
          }

          const parent3El = parent2El?.parentElement;
          if (parent3El) {
            parent3El.classList.remove('mm-active');

            if (parent3El.classList.contains('side-nav-item')) {
              const firstAnchor = parent3El.querySelector('.side-nav-link-a-ref');

              if (firstAnchor) {
                firstAnchor.classList.remove('mm-active');
              }
            }

            const parent4El = parent3El.parentElement;
            if (parent4El) {
              parent4El.classList.remove('mm-show');

              const parent5El = parent4El.parentElement;
              if (parent5El) {
                parent5El.classList.remove('mm-active');
              }
            }
          }
      }
    }
  };


  /**
   * Toggles the state of the menu items
   */
  activateMenuItems() {

    const links: any = document.getElementsByClassName('nav-link-ref');

    let menuItemEl = null;

    for (let i = 0; i < links.length; i++) {
      // tslint:disable-next-line: no-string-literal
        if (window.location.pathname === links[i]['pathname']) {

            menuItemEl = links[i];

            break;
        }
    }

    if (menuItemEl) {
        menuItemEl.classList.add('mm-active');
        const parentEl = menuItemEl.parentElement;

        if (parentEl) {
            parentEl.classList.add('mm-active');

            const parent2El = parentEl.parentElement;
            if (parent2El) {
                parent2El.classList.add('mm-show');
            }

            const parent3El = parent2El.parentElement;
            if (parent3El) {
                parent3El.classList.add('mm-active');

                if (parent3El.classList.contains('side-nav-item')) {
                    const firstAnchor = parent3El.querySelector('.side-nav-link-a-ref');

                    if (firstAnchor) {
                        firstAnchor.classList.add('mm-active');
                    }
                }

                const parent4El = parent3El.parentElement;
                if (parent4El) {
                    parent4El.classList.add('mm-show');

                    const parent5El = parent4El.parentElement;
                    if (parent5El) {
                        parent5El.classList.add('mm-active');
                    }
                }
            }
        }
    }
  };

  triggerPDFRefresh() {
    this.sharedService.callAction('refreshpdf');
  }

}
