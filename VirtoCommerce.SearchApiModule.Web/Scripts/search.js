var moduleName = "virtoCommerce.searchModuleApi";

if (AppDependencies != undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, ['ngSanitize', 'virtoCommerce.catalogModule'])
.run(
  ['platformWebApp.widgetService', 'virtoCommerce.catalogModule.predefinedSearchFilters', function (widgetService, predefinedSearchFilters) {
      // filter properties in STORE details
      widgetService.registerWidget({
          controller: 'virtoCommerce.searchModuleApi.storePropertiesWidgetController',
          template: 'Modules/$(VirtoCommerce.SearchApi)/Scripts/widgets/storePropertiesWidget.tpl.html'
      }, 'storeDetail');

      // predefine search filters for catalog search
      predefinedSearchFilters.register(1477584000000, 'catalogSearchFiltersDate-searchAPI', [
        { keyword: 'is:hidden', id: 4, name: 'searchApi.filter-labels.filter-notActive' },
        { keyword: 'price_usd:[100 TO 200]', id: 3, name: 'searchApi.filter-labels.filter-priceRange' },
        { keyword: 'is:priced', id: 2, name: 'searchApi.filter-labels.filter-withPrice' },
        { keyword: 'is:unpriced', id: 1, name: 'searchApi.filter-labels.filter-priceless' }
      ]);
  }]
);
