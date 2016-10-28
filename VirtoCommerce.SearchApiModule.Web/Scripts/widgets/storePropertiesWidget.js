angular.module('virtoCommerce.searchModuleApi')
.controller('virtoCommerce.searchModuleApi.storePropertiesWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
    var blade = $scope.blade;

    $scope.openBlade = function () {
        var newBlade = {
            id: "storeFilteringProperties",
            storeId: blade.currentEntity.id,
            title: 'Filtering properties',
            controller: 'virtoCommerce.searchModuleApi.storePropertiesController',
            template: 'Modules/$(VirtoCommerce.SearchApi)/Scripts/blades/store-properties.tpl.html'
        };
        bladeNavigationService.showBlade(newBlade, blade);
    };
}]);
