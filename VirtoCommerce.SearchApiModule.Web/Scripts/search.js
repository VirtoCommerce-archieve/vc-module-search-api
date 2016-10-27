var moduleName = "virtoCommerce.searchModuleApi";

if (AppDependencies != undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [
    'ngSanitize'
])
.run(
  ['platformWebApp.widgetService', function (widgetService) {
      // filter properties in STORE details
      widgetService.registerWidget({
          controller: 'virtoCommerce.searchModuleApi.storePropertiesWidgetController',
          template: 'Modules/$(VirtoCommerce.SearchApi)/Scripts/widgets/storePropertiesWidget.tpl.html'
      }, 'storeDetail');
  }]
);
