var moduleName = "virtoCommerce.searchModuleApi";

if (AppDependencies != undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, ['ngSanitize', 'virtoCommerce.catalogModule'])
.run(
  ['platformWebApp.widgetService', 'virtoCommerce.catalogModule.predefinedSearchFilters', 'platformWebApp.toolbarService', 'virtoCommerce.searchModule.search', 'platformWebApp.dialogService', 'platformWebApp.bladeNavigationService', function (widgetService, predefinedSearchFilters, toolbarService, searchAPI, dialogService, bladeNavigationService) {
      // filter properties in STORE details
      widgetService.registerWidget({
          controller: 'virtoCommerce.searchModuleApi.storePropertiesWidgetController',
          template: 'Modules/$(VirtoCommerce.SearchApi)/Scripts/widgets/storePropertiesWidget.tpl.html'
      }, 'storeDetail');

      // toolbar button 'rebuild'
      var rebuildIndexCommand = {
          name: "search.commands.rebuild-index",
          icon: 'fa fa-recycle',
          index: 2,
          executeMethod: function (blade) {
              var dialog = {
                  id: "confirmRebuildIndex",
                  callback: function (doReindex) {
                      var apiToCall = doReindex ? searchAPI.reindex : searchAPI.index;
                      var documentsIds = blade.currentEntityId ? [{ id: blade.currentEntityId }] : undefined;
                      apiToCall({ documentType: blade.documentType }, documentsIds,
                              function openProgressBlade(data) {
                                  // show indexing progress
                                  var newBlade = {
                                      id: 'indexProgress',
                                      notification: data,
                                      parentRefresh: blade.parentRefresh,
                                      controller: 'virtoCommerce.searchModule.indexProgressController',
                                      template: 'Modules/$(VirtoCommerce.Search)/Scripts/blades/index-progress.tpl.html'
                                  };
                                  bladeNavigationService.showBlade(newBlade, blade.parentBlade || blade);
                              });
                  }
              }
              dialogService.showDialog(dialog, 'Modules/$(VirtoCommerce.SearchApi)/Scripts/dialogs/reindex-dialog.tpl.html', 'platformWebApp.confirmDialogController');
          },
          canExecuteMethod: function () { return true; },
          permission: 'VirtoCommerce.Search:Index:Rebuild'
      };

      // register in catalogs list
      toolbarService.register(rebuildIndexCommand, 'virtoCommerce.catalogModule.catalogsListController');

      // register WIDGETS
      var indexWidget = {
          controller: 'virtoCommerce.searchModule.indexWidgetController',
          // size: [3, 1],
          template: 'Modules/$(VirtoCommerce.Search)/Scripts/widgets/common/index-widget.tpl.html'
      };

      // integration: index in product details
      var widgetToRegister = angular.extend({}, indexWidget, { documentType: 'catalogitem' })
      widgetService.registerWidget(widgetToRegister, 'itemDetail');
      // integration: index in CATEGORY details
      widgetToRegister = angular.extend({}, indexWidget, { documentType: 'category' })
      widgetService.registerWidget(widgetToRegister, 'categoryDetail');
      // integration: index in catalog details
      //widgetToRegister = angular.extend({}, indexWidget, { documentType: 'catalog' })
      //widgetService.registerWidget(widgetToRegister, 'catalogDetail');

      // predefine search filters for catalog search
      predefinedSearchFilters.register(1477584000000, 'catalogSearchFiltersDate-searchAPI', [
        { keyword: 'is:hidden', id: 4, name: 'searchApi.filter-labels.filter-notActive' },
        { keyword: 'price_usd:[100 TO 200]', id: 3, name: 'searchApi.filter-labels.filter-priceRange' },
        { keyword: 'is:priced', id: 2, name: 'searchApi.filter-labels.filter-withPrice' },
        { keyword: 'is:unpriced', id: 1, name: 'searchApi.filter-labels.filter-priceless' }
      ]);
  }]
);
