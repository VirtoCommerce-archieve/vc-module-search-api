angular.module('virtoCommerce.searchAPIModule')
.controller('virtoCommerce.searchAPIModule.catalogIndexWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.searchAPIModule.searchAPIResources', function ($scope, bladeNavigationService, searchAPI) {
    var blade = $scope.blade;
    $scope.loading = true;

    // searchAPI.getStatistics({ catalogId: blade.currentEntityId }, function (data) {
    _.each([{ id: blade.currentEntityId, itemCount: 15, categoryCount: 7, itemCountCatalog: 315, categoryCountCatalog: 76 }], function (data) {
        $scope.index = data;
        $scope.loading = false;
        if (!$scope.index.id) {
            $scope.widget.UIclass = 'error';
        } else if ($scope.index.itemCount < $scope.index.itemCountCatalog || $scope.index.categoryCount < $scope.index.categoryCountCatalog)
            $scope.widget.UIclass = 'error';
    });

    $scope.openBlade = function () {
        var newBlade = {
            id: 'detailChild',
            isCatalog: true,
            currentEntityId: blade.currentEntityId,
            data: $scope.index,
            parentRefresh : blade.parentRefresh,
            title: blade.currentEntity.name,
            subtitle: 'searchAPI.blades.index-detail.subtitle-catalog',
            controller: 'virtoCommerce.searchAPIModule.indexDetailController',
            template: 'Modules/$(VirtoCommerce.SearchApi)/Scripts/blades/index-detail.tpl.html'
        };
        bladeNavigationService.showBlade(newBlade, blade);
    };
}]);
